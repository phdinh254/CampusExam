using System.Security.Claims;
using CampusExam.Api.Authorization;
using CampusExam.Api.Controllers;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.QuestionImports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class QuestionImportsControllerTests
{
    [Fact]
    public async Task Preview_returns_service_result_and_passes_file_name_and_user()
    {
        var expected = new QuestionImportPreview(0, 0, 0, 0, []);
        var service = new FakeImportService(expected);
        var controller = TaoController(service);
        var file = CreateFile("questions.docx", [1, 2, 3]);

        var result = await controller.Preview(file, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
        Assert.Equal("questions.docx", service.TenFile);
        Assert.Equal(42, service.Nguoi?.MaTK);
    }

    [Fact]
    public async Task Preview_rejects_legacy_doc_before_calling_service()
    {
        var service = new FakeImportService(new QuestionImportPreview(0, 0, 0, 0, []));
        var controller = TaoController(service);
        var file = CreateFile("questions.doc", [1, 2, 3]);

        var result = await controller.Preview(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(
            "QUESTION_IMPORT_UNSUPPORTED_FILE_TYPE",
            problem.Extensions["code"]);
        Assert.Null(service.TenFile);
    }

    private static QuestionImportsController TaoController(IQuestionImportService service) => new(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(CampusClaims.MaTaiKhoan, "42")], "test"))
            }
        }
    };

    private static IFormFile CreateFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName);
    }

    private sealed class FakeImportService(QuestionImportPreview result) : IQuestionImportService
    {
        public string? TenFile { get; private set; }

        public NguoiThucHien? Nguoi { get; private set; }

        public Task<QuestionImportPreview> XemTruocAsync(
            Stream docx,
            string tenFile,
            NguoiThucHien nguoi,
            CancellationToken cancellationToken)
        {
            TenFile = tenFile;
            Nguoi = nguoi;
            return Task.FromResult(result);
        }

        public Task<KetQua<LanImportDto>> XacNhanAsync(string maXemTruoc, NguoiThucHien nguoi, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<TrangDuLieu<LanImportDto>> GetPageAsync(TruyVanTrang truyVan, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LanImportDto?> GetByIdAsync(int maLanImport, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
