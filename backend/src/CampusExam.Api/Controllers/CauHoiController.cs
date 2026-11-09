using CampusExam.Api.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.CauHoi;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

/// <summary>Ngân hàng câu hỏi cho quản trị viên (docs/API_DESIGN.md mục 2.4).</summary>
[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/cau-hoi")]
public class CauHoiController(
    ICauHoiQuery cauHoiQuery,
    ICauHoiCommandService cauHoiCommandService,
    IFileStorage fileStorage) : ControllerBase
{
    /// <summary>
    /// Ảnh trong nội dung/phương án. Key lưu trữ bất biến nên cache được; CSP sandbox + nosniff để
    /// tệp SVG/HTML giả dạng ảnh không chạy được script nếu mở trực tiếp.
    /// </summary>
    [HttpGet("{maCH:int}/hinh-anh/{maTN:int}")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHinhAnh(int maCH, int maTN, CancellationToken cancellationToken)
    {
        var hinh = await cauHoiQuery.GetHinhAnhAsync(maCH, maTN, cancellationToken);
        var stream = hinh is null ? null : await fileStorage.OpenReadAsync(hinh.StorageKey, cancellationToken);
        if (hinh is null || stream is null)
        {
            return NotFound(ApiProblem.Create(HttpContext, StatusCodes.Status404NotFound, "Không tìm thấy hình",
                ErrorCodes.QuestionMediaNotFound, $"Câu hỏi {maCH} không có hình {maTN}."));
        }

        Response.Headers.CacheControl = "private, max-age=86400, immutable";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        return File(stream, hinh.MimeType);
    }

    [HttpGet]
    [ProducesResponseType<TrangDuLieu<CauHoiTomTatDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrangDuLieu<CauHoiTomTatDto>>> GetPage(
        [FromQuery] TruyVanCauHoi truyVan,
        CancellationToken cancellationToken)
    {
        return Ok(await cauHoiQuery.GetPageAsync(truyVan, cancellationToken));
    }

    [HttpGet("thong-ke")]
    [ProducesResponseType<ThongKeCauHoiDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ThongKeCauHoiDto>> GetThongKe(CancellationToken cancellationToken)
    {
        return Ok(await cauHoiQuery.GetThongKeAsync(cancellationToken));
    }

    [HttpGet("{maCH:int}")]
    [ProducesResponseType<CauHoiChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CauHoiChiTietDto>> GetById(int maCH, CancellationToken cancellationToken)
    {
        var cauHoi = await cauHoiQuery.GetByIdAsync(maCH, cancellationToken);
        if (cauHoi is null)
        {
            return NotFound(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                "Không tìm thấy câu hỏi",
                ErrorCodes.QuestionNotFound,
                $"Không tồn tại câu hỏi có mã {maCH}."));
        }

        return Ok(cauHoi);
    }

    [HttpPost]
    [ProducesResponseType<CauHoiChiTietDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CauHoiChiTietDto>> Create(
        TaoCauHoiCommand command,
        CancellationToken cancellationToken)
    {
        var ketQua = await cauHoiCommandService.TaoAsync(command, cancellationToken);
        if (!ketQua.ThanhCong)
        {
            return ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
        }

        return CreatedAtAction(nameof(GetById), new { maCH = ketQua.GiaTri!.MaCH }, ketQua.GiaTri);
    }

    /// <summary>Sửa nội dung; bắt buộc gửi <c>phienBan</c>, lệch trả 409 CONCURRENCY_CONFLICT.</summary>
    [HttpPut("{maCH:int}")]
    [ProducesResponseType<CauHoiChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CauHoiChiTietDto>> Update(
        int maCH,
        SuaCauHoiCommand command,
        CancellationToken cancellationToken)
    {
        var ketQua = await cauHoiCommandService.SuaAsync(maCH, command, cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    [HttpPatch("{maCH:int}/trang-thai")]
    [ProducesResponseType<CauHoiChiTietDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CauHoiChiTietDto>> DoiTrangThai(
        int maCH,
        DoiTrangThaiRequest request,
        CancellationToken cancellationToken)
    {
        var ketQua = await cauHoiCommandService.DoiTrangThaiAsync(maCH, request.TrangThai!.Value, cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    /// <summary>Xóa cứng câu hỏi chưa nằm trong đề nào; ngược lại 409 QUESTION_IN_USE.</summary>
    [HttpDelete("{maCH:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int maCH, CancellationToken cancellationToken)
    {
        var ketQua = await cauHoiCommandService.XoaAsync(maCH, cancellationToken);
        return ketQua.ThanhCong ? NoContent() : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }
}
