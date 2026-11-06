using CampusExam.Api.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.Auth;
using CampusExam.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CampusExam.Api.Controllers;

/// <summary>Đăng nhập, phiên và mật khẩu — docs/API_DESIGN.md mục 1.5, 2.1.</summary>
[ApiController]
[Route("api/xac-thuc")]
[ChoPhepKhiPhaiDoiMatKhau]
public sealed class XacThucController(
    IAuthService authService,
    IOptions<AuthOptions> authOptions) : ControllerBase
{
    public const string RefreshCookie = "ce_refresh";
    public const string RefreshCookiePath = "/api/xac-thuc";
    public const string CsrfHeader = "X-CSRF";

    [HttpPost("dang-nhap")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.DangNhap)]
    [ProducesResponseType<PhienDangNhapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PhienDangNhapResponse>> DangNhap(
        DangNhapRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.DangNhapAsync(request, NguCanh(), cancellationToken);

        switch (result.KetQua)
        {
            case KetQuaDangNhap.ThanhCong:
                GhiCookie(result.Phien!);
                return Ok(result.Phien!.ToResponse());

            case KetQuaDangNhap.TamKhoa:
                var locked = ApiProblem.Create(HttpContext, StatusCodes.Status423Locked,
                    "Tài khoản tạm khóa do đăng nhập sai nhiều lần", ErrorCodes.AuthAccountLocked,
                    "Vui lòng thử lại sau ít phút.");
                locked.Extensions["khoaDen"] = result.KhoaDen;
                return StatusCode(StatusCodes.Status423Locked, locked);

            case KetQuaDangNhap.BiVoHieuHoa:
                return StatusCode(StatusCodes.Status403Forbidden, ApiProblem.Create(HttpContext,
                    StatusCodes.Status403Forbidden, "Tài khoản đã bị khóa", ErrorCodes.AuthAccountDisabled,
                    "Liên hệ quản trị viên để được mở khóa."));

            default:
                return Unauthorized(ApiProblem.Create(HttpContext, StatusCodes.Status401Unauthorized,
                    "Tên đăng nhập hoặc mật khẩu không đúng", ErrorCodes.AuthInvalidCredentials));
        }
    }

    [HttpPost("lam-moi")]
    [AllowAnonymous]
    [ProducesResponseType<PhienDangNhapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PhienDangNhapResponse>> LamMoi(CancellationToken cancellationToken)
    {
        if (!YeuCauCungNguon())
        {
            return LoiCsrf();
        }

        var refreshToken = Request.Cookies[RefreshCookie];
        var phien = string.IsNullOrEmpty(refreshToken)
            ? null
            : await authService.LamMoiAsync(refreshToken, NguCanh(), cancellationToken);

        if (phien is null)
        {
            XoaCookie();
            return Unauthorized(ApiProblem.Create(HttpContext, StatusCodes.Status401Unauthorized,
                "Phiên đăng nhập đã hết hạn", ErrorCodes.AuthRefreshInvalid));
        }

        GhiCookie(phien);
        return Ok(phien.ToResponse());
    }

    [HttpPost("dang-xuat")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DangXuat(CancellationToken cancellationToken)
    {
        if (!YeuCauCungNguon())
        {
            return LoiCsrf();
        }

        var refreshToken = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.DangXuatAsync(refreshToken, NguCanh(), cancellationToken);
        }

        XoaCookie();
        return NoContent();
    }

    [HttpGet("toi")]
    [Authorize]
    [ProducesResponseType<TaiKhoanHienTaiDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaiKhoanHienTaiDto>> Toi(CancellationToken cancellationToken)
    {
        var taiKhoan = await authService.LayTaiKhoanAsync(MaTaiKhoan(), cancellationToken);
        return taiKhoan is null
            ? Unauthorized(ApiProblem.Create(HttpContext, StatusCodes.Status401Unauthorized,
                "Chưa đăng nhập", ErrorCodes.AuthUnauthenticated))
            : Ok(taiKhoan);
    }

    [HttpPost("doi-mat-khau")]
    [Authorize]
    // Dùng chung hạn mức với đăng nhập: chặn dò mật khẩu cũ bằng access token bị lộ.
    [EnableRateLimiting(RateLimitPolicies.DangNhap)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<PhienDangNhapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PhienDangNhapResponse>> DoiMatKhau(
        DoiMatKhauRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.DoiMatKhauAsync(
            MaTaiKhoan(), request, Request.Cookies[RefreshCookie], NguCanh(), cancellationToken);

        switch (result.KetQua)
        {
            case KetQuaDoiMatKhau.SaiMatKhauCu:
                return BadRequest(ApiProblem.Validation(HttpContext,
                    new Dictionary<string, string[]> { ["matKhauCu"] = ["Mật khẩu hiện tại không đúng."] },
                    ErrorCodes.AuthCurrentPasswordIncorrect, "Mật khẩu hiện tại không đúng"));

            case KetQuaDoiMatKhau.MatKhauMoiKhongHopLe:
                return BadRequest(ApiProblem.Validation(HttpContext,
                    new Dictionary<string, string[]> { ["matKhauMoi"] = [.. result.Loi!] }));

            default:
                GhiCookie(result.Phien!);
                return Ok(result.Phien!.ToResponse());
        }
    }

    private int MaTaiKhoan() => int.Parse(User.FindFirst(CampusClaims.MaTaiKhoan)!.Value);

    private NguCanhYeuCau NguCanh() => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString());

    /// <summary>
    /// Chống CSRF cho các lệnh dùng cookie: bắt buộc header tự đặt (trình duyệt khác origin phải preflight,
    /// mà API không bật CORS) và Origin, nếu có, phải thuộc allowlist hoặc trùng chính API.
    /// </summary>
    private bool YeuCauCungNguon()
    {
        if (Request.Headers[CsrfHeader] != "1")
        {
            return false;
        }

        var origin = Request.Headers.Origin.ToString().TrimEnd('/');
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        return origin.Equals($"{Request.Scheme}://{Request.Host}", StringComparison.OrdinalIgnoreCase)
               || authOptions.Value.AllowedOrigins.Any(allowed =>
                   allowed.TrimEnd('/').Equals(origin, StringComparison.OrdinalIgnoreCase));
    }

    private ObjectResult LoiCsrf() => StatusCode(StatusCodes.Status403Forbidden, ApiProblem.Create(HttpContext,
        StatusCodes.Status403Forbidden, "Yêu cầu không hợp lệ", ErrorCodes.AuthCsrfFailed));

    private void GhiCookie(PhienDaCap phien) =>
        Response.Cookies.Append(RefreshCookie, phien.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            IsEssential = true,
            // Không ghi nhớ: cookie phiên trình duyệt, máy chủ vẫn giới hạn bằng HETHAN.
            Expires = phien.GhiNho ? new DateTimeOffset(phien.RefreshTokenHetHan) : null
        });

    private void XoaCookie() =>
        Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
}
