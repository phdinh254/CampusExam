using CampusExam.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/kqht")]
public class KqhtController(
    IKqhtQuery kqhtQuery,
    IKqhtCommandService kqhtCommandService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KqhtDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var danhSach = await kqhtQuery.GetAllAsync(cancellationToken);

        return Ok(danhSach);
    }

    [HttpGet("{maKQHT:int}")]
    [ProducesResponseType<KqhtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KqhtDto>> GetById(
        int maKQHT,
        CancellationToken cancellationToken)
    {
        var kqht = await kqhtQuery.GetByIdAsync(
            maKQHT,
            cancellationToken);

        if (kqht is null)
        {
            return NotFound(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status404NotFound,
                "Không tìm thấy KQHT",
                ErrorCodes.KqhtNotFound,
                $"Không tồn tại KQHT có mã {maKQHT}."));
        }

        return Ok(kqht);
    }

    [HttpPost]
    [ProducesResponseType<KqhtDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KqhtDto>> Create(
        CreateKqhtCommand command,
        CancellationToken cancellationToken)
    {
        var result = await kqhtCommandService.CreateAsync(
            command,
            cancellationToken);

        if (result.Status == CreateKqhtStatus.DuplicateKyHieu)
        {
            return Conflict(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status409Conflict,
                "Ký hiệu KQHT đã tồn tại",
                ErrorCodes.KqhtKyHieuConflict,
                $"Đã tồn tại KQHT có ký hiệu {command.KyHieu.ToUpperInvariant()}."));
        }

        var created = result.Kqht!;

        return CreatedAtAction(
            nameof(GetById),
            new { maKQHT = created.MaKQHT },
            created);
    }

    /// <summary>Sửa tên, mô tả, thứ tự; đã có câu hỏi thì không đổi được thứ tự (409 KQHT_IN_USE).</summary>
    [HttpPut("{maKQHT:int}")]
    [ProducesResponseType<KqhtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KqhtDto>> Update(
        int maKQHT,
        UpdateKqhtCommand command,
        CancellationToken cancellationToken)
    {
        var ketQua = await kqhtCommandService.UpdateAsync(maKQHT, command, cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    [HttpPatch("{maKQHT:int}/trang-thai")]
    [ProducesResponseType<KqhtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KqhtDto>> DoiTrangThai(
        int maKQHT,
        DoiTrangThaiRequest request,
        CancellationToken cancellationToken)
    {
        var ketQua = await kqhtCommandService.DoiTrangThaiAsync(maKQHT, request.TrangThai!.Value, cancellationToken);
        return ketQua.ThanhCong ? Ok(ketQua.GiaTri) : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }

    /// <summary>Xóa cứng KQHT chưa được câu hỏi hoặc ma trận nào dùng; ngược lại 409 KQHT_IN_USE.</summary>
    [HttpDelete("{maKQHT:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int maKQHT,
        CancellationToken cancellationToken)
    {
        var ketQua = await kqhtCommandService.XoaAsync(maKQHT, cancellationToken);
        return ketQua.ThanhCong ? NoContent() : ApiProblem.FromLoi(HttpContext, ketQua.Loi!);
    }
}
