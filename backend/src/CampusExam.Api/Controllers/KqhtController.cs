using CampusExam.Api.Common;
using CampusExam.Application.Common;
using CampusExam.Application.Kqht;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

[ApiController]
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
}
