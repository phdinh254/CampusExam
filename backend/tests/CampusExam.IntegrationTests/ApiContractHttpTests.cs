using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CampusExam.IntegrationTests.Api;
using CampusExam.IntegrationTests.Database;
using Xunit;

namespace CampusExam.IntegrationTests;

/// <summary>
/// Kiểm tra hợp đồng HTTP qua pipeline thật (routing, model binding, ProblemDetails, OpenAPI)
/// với tài khoản quản trị đã đăng nhập.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ApiContractHttpTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    private readonly HttpClient _client = CampusExamApiFactory.For(database).TaoClient();
    private string _token = string.Empty;

    public async Task InitializeAsync()
    {
        await database.ResetAsync();
        await DuLieuXacThuc.TaoAdminAsync(database);
        _token = await _client.LayTokenAdminAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Validation_error_has_stable_code_and_trace_id()
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/kqht", _token, body: new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_FAILED", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("Dữ liệu không hợp lệ", problem.RootElement.GetProperty("title").GetString());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("traceId").GetString()));
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("tenKQHT", out _));
    }

    [Theory]
    [InlineData("\"active\"")]
    [InlineData("1")]
    public async Task Kqht_status_only_accepts_vietnamese_strings(string trangThaiJson)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/kqht")
        {
            Content = new StringContent(
                $$"""{"tenKQHT":"Nhớ","thuTu":1,"kyHieu":"A","trangThai":{{trangThaiJson}}}""",
                MediaTypeHeaderValue.Parse("application/json"))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(raw);
        Assert.Equal("VALIDATION_FAILED", problem.RootElement.GetProperty("code").GetString());

        var errors = problem.RootElement.GetProperty("errors");
        Assert.Equal("Giá trị không hợp lệ.", errors.GetProperty("trangThai")[0].GetString());
        // Lỗi đọc JSON không được kéo theo lỗi "command is required" hay lộ tên kiểu nội bộ.
        Assert.False(errors.TryGetProperty("command", out _));
        Assert.DoesNotContain("CampusExam.", raw);
    }

    [Fact]
    public async Task Kqht_created_through_api_returns_status_string()
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/kqht", _token,
            body: new { tenKQHT = "Nhớ", thuTu = 1, kyHieu = "A" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal("dang_su_dung", body.RootElement.GetProperty("trangThai").GetString());
    }

    [Fact]
    public async Task Question_import_preview_is_served_at_vietnamese_route()
    {
        var response = await PostFileAsync("/api/nhap-cau-hoi/xem-truoc", "cau-hoi.doc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await ReadJsonAsync(response);
        Assert.Equal(
            "QUESTION_IMPORT_UNSUPPORTED_FILE_TYPE",
            problem.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Old_english_import_route_is_removed()
    {
        var response = await PostFileAsync("/api/question-imports/preview", "cau-hoi.docx");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_lists_vietnamese_routes_in_development()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/nhap-cau-hoi/xem-truoc", out _));
        Assert.True(paths.TryGetProperty("/api/kqht", out _));
        Assert.True(paths.TryGetProperty("/api/muc-do", out _));
        Assert.True(paths.TryGetProperty("/api/xac-thuc/dang-nhap", out _));
        Assert.False(paths.TryGetProperty("/api/question-imports/preview", out _));
    }

    private Task<HttpResponseMessage> PostFileAsync(string url, string fileName)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(file, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return _client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
