using CampusExam.Api.Controllers;
using CampusExam.Application.QuestionImports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class QuestionImportsControllerTests
{
    [Fact]
    public async Task Preview_returns_parser_result_for_docx_file()
    {
        var expected = new QuestionImportPreview(0, 0, 0, 0, []);
        var parser = new FakeParser(expected);
        var controller = new QuestionImportsController(parser);
        var file = CreateFile("questions.docx", [1, 2, 3]);

        var result = await controller.Preview(file, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
        Assert.True(parser.WasCalled);
    }

    [Fact]
    public async Task Preview_rejects_legacy_doc_before_calling_parser()
    {
        var parser = new FakeParser(new QuestionImportPreview(0, 0, 0, 0, []));
        var controller = new QuestionImportsController(parser);
        var file = CreateFile("questions.doc", [1, 2, 3]);

        var result = await controller.Preview(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(
            "QUESTION_IMPORT_UNSUPPORTED_FILE_TYPE",
            problem.Extensions["code"]);
        Assert.False(parser.WasCalled);
    }

    private static IFormFile CreateFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName);
    }

    private sealed class FakeParser(
        QuestionImportPreview result) : IQuestionDocxParser
    {
        public bool WasCalled { get; private set; }

        public Task<QuestionImportPreview> PreviewAsync(
            Stream docxStream,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(result);
        }
    }
}
