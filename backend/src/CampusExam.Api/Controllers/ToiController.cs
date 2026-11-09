using CampusExam.Api.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.Common;
using CampusExam.Application.ThiSinh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

/// <summary>
/// Không gian của thí sinh đang đăng nhập (docs/API_DESIGN.md mục 2.11). Luôn lấy thí sinh từ
/// token, không nhận MSTS trên URL.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.CandidateOnly)]
[Route("api/toi")]
public class ToiController(IThiSinhQuery thiSinhQuery) : ControllerBase
{
    /// <summary>Hồ sơ cá nhân cho trang "Tài khoản cá nhân". Thí sinh không tự sửa hồ sơ (D-THISINH-01).</summary>
    [HttpGet("ho-so")]
    [ProducesResponseType<HoSoCaNhanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HoSoCaNhanDto>> GetHoSo(CancellationToken cancellationToken)
    {
        var msts = User.FindFirst(CampusClaims.Msts)!.Value;
        var thiSinh = await thiSinhQuery.GetByMstsAsync(msts, cancellationToken);
        if (thiSinh is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, StatusCodes.Status404NotFound, "Không tìm thấy hồ sơ",
                ErrorCodes.StudentNotFound, "Tài khoản không còn gắn với hồ sơ thí sinh."));
        }

        return Ok(new HoSoCaNhanDto(
            thiSinh.Msts,
            thiSinh.HoTen,
            thiSinh.NgaySinh,
            thiSinh.GioiTinh,
            thiSinh.Lop,
            thiSinh.NoiSinh,
            thiSinh.Cccd,
            thiSinh.NgayTao));
    }
}
