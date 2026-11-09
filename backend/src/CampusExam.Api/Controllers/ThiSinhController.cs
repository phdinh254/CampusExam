using CampusExam.Api.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.ThiSinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

/// <summary>Hồ sơ và tài khoản thí sinh (docs/API_DESIGN.md mục 2.6, D-THISINH-01).</summary>
[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/thi-sinh")]
public class ThiSinhController(
    IThiSinhQuery thiSinhQuery,
    IThiSinhCommandService thiSinhCommandService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TrangDuLieu<ThiSinhTomTatDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrangDuLieu<ThiSinhTomTatDto>>> GetPage(
        [FromQuery] TruyVanThiSinh truyVan,
        CancellationToken cancellationToken) =>
        Ok(await thiSinhQuery.GetPageAsync(truyVan, cancellationToken));

    [HttpGet("lop")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetLop(CancellationToken cancellationToken) =>
        Ok(await thiSinhQuery.GetLopAsync(cancellationToken));

    [HttpGet("{msts}")]
    [ProducesResponseType<ThiSinhChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThiSinhChiTietDto>> GetByMsts(string msts, CancellationToken cancellationToken)
    {
        var thiSinh = await thiSinhQuery.GetByMstsAsync(msts, cancellationToken);
        return thiSinh is null
            ? NotFound(ApiProblem.Create(HttpContext, StatusCodes.Status404NotFound, "Không tìm thấy thí sinh",
                ErrorCodes.StudentNotFound, $"Không tồn tại thí sinh có mã số {msts}."))
            : Ok(thiSinh);
    }

    /// <summary>Tạo kèm tài khoản: tên đăng nhập = MSTS, mật khẩu mặc định = ngày sinh ddMMyyyy.</summary>
    [HttpPost]
    [ProducesResponseType<ThiSinhChiTietDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ThiSinhChiTietDto>> Create(TaoThiSinhCommand command, CancellationToken cancellationToken)
    {
        var ketQua = await thiSinhCommandService.TaoAsync(command, NguoiThucHien(), cancellationToken);
        return ketQua.ThanhCong
            ? CreatedAtAction(nameof(GetByMsts), new { msts = ketQua.GiaTri!.Msts }, ketQua.GiaTri)
            : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    [HttpPut("{msts}")]
    [ProducesResponseType<ThiSinhChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ThiSinhChiTietDto>> Update(string msts, SuaThiSinhCommand command, CancellationToken cancellationToken)
    {
        var ketQua = await thiSinhCommandService.SuaAsync(msts, command, NguoiThucHien(), cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    [HttpPatch("{msts}/trang-thai-tai-khoan")]
    [ProducesResponseType<ThiSinhChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThiSinhChiTietDto>> DoiTrangThaiTaiKhoan(
        string msts,
        DoiTrangThaiTaiKhoanRequest request,
        CancellationToken cancellationToken)
    {
        var ketQua = await thiSinhCommandService.DoiTrangThaiTaiKhoanAsync(
            msts, request.TrangThai!.Value, NguoiThucHien(), cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    /// <summary>Đặt lại mật khẩu về ngày sinh, bắt buộc đổi, thu hồi mọi phiên. Không trả mật khẩu.</summary>
    [HttpPost("{msts}/cap-lai-mat-khau")]
    [ProducesResponseType<ThiSinhChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThiSinhChiTietDto>> CapLaiMatKhau(string msts, CancellationToken cancellationToken)
    {
        var ketQua = await thiSinhCommandService.CapLaiMatKhauAsync(msts, NguoiThucHien(), cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    /// <summary>Xóa thí sinh chưa được phân công thi; ngược lại 409 STUDENT_IN_USE.</summary>
    [HttpDelete("{msts}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string msts, CancellationToken cancellationToken)
    {
        var ketQua = await thiSinhCommandService.XoaAsync(msts, NguoiThucHien(), cancellationToken);
        return ketQua.ThanhCong ? NoContent() : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    private NguoiThucHien NguoiThucHien() => new(
        int.Parse(User.FindFirst(CampusClaims.MaTaiKhoan)!.Value),
        HttpContext.Connection.RemoteIpAddress?.ToString());
}
