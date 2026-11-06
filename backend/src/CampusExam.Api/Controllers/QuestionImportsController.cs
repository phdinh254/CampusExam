using CampusExam.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using CampusExam.Api.Common;
using CampusExam.Application.Common;
using CampusExam.Application.QuestionImports;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/nhap-cau-hoi")]
public sealed class QuestionImportsController(
    IQuestionDocxParser parser) : ControllerBase
{
    private const long MaximumFileSize = 20 * 1024 * 1024;

    [HttpPost("xem-truoc")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumFileSize)]
    [ProducesResponseType<QuestionImportPreview>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<QuestionImportPreview>> Preview(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "Tệp import bị trống",
                ErrorCodes.QuestionImportEmptyFile));
        }

        if (file.Length > MaximumFileSize)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiProblem.Create(
                    HttpContext,
                    StatusCodes.Status413PayloadTooLarge,
                    "Tệp import vượt quá 20 MB",
                    ErrorCodes.QuestionImportFileTooLarge));
        }

        if (!Path.GetExtension(file.FileName).Equals(
                ".docx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "Chỉ hỗ trợ định dạng DOCX",
                ErrorCodes.QuestionImportUnsupportedFileType,
                "Tệp DOC cũ cần được chuyển đổi sang DOCX trước khi import."));
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        try
        {
            var preview = await parser.PreviewAsync(stream, cancellationToken);
            return Ok(preview);
        }
        catch (QuestionImportFormatException exception)
        {
            return BadRequest(ApiProblem.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                "Không đọc được tệp DOCX",
                ErrorCodes.QuestionImportInvalidFile,
                exception.Message));
        }
    }
}
