using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CampusExam.IntegrationTests.Database;
using CampusExam.IntegrationTests.Imports;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CampusExam.IntegrationTests.Api;

/// <summary>
/// Nhập câu hỏi DOCX hai bước (API_DESIGN.md mục 2.5): D-IMPORT-01 (chỉ ghi khi hết lỗi),
/// D-IMPORT-TRUNG-01 (mã trùng database chặn cả tệp), D-MEDIA-01 (hình ở storage).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class NhapCauHoiApiTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    private readonly CampusExamApiFactory _factory = CampusExamApiFactory.For(database);
    private HttpClient _client = null!;
    private string _token = string.Empty;

    public async Task InitializeAsync()
    {
        await database.ResetAsync();
        _client = _factory.TaoClient();
        // Storage dùng chung trong host: dọn để test không phụ thuộc thứ tự chạy.
        foreach (var thuMuc in Directory.Exists(_factory.StorageRoot) ? Directory.GetDirectories(_factory.StorageRoot) : [])
        {
            Directory.Delete(thuMuc, recursive: true);
        }

        await DuLieuXacThuc.TaoAdminAsync(database);
        _token = await _client.LayTokenAdminAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Valid_file_is_previewed_then_confirmed_with_questions_and_images()
    {
        await TaoKqhtAsync(1);
        await TaoKqhtAsync(2);

        using var xemTruoc = await XemTruocAsync(
            new DongMau("A1-5", "Khóa chính là gì?", "B", ImagePartType.Png),
            new DongMau("B2-1", "Chuẩn hóa 3NF", "D"));

        var root = xemTruoc.RootElement;
        Assert.Equal(2, root.GetProperty("soDongHopLe").GetInt32());
        Assert.Equal(1, root.GetProperty("tongHinhAnh").GetInt32());
        var maXemTruoc = root.GetProperty("maXemTruoc").GetString()!;
        var hetHan = root.GetProperty("hetHan").GetDateTime();
        Assert.InRange(hetHan - DateTime.UtcNow, TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(31));

        var xacNhan = await XacNhanAsync(maXemTruoc);

        Assert.Equal(HttpStatusCode.Created, xacNhan.StatusCode);
        int maLanImport;
        using (var lan = await DocAsync(xacNhan))
        {
            maLanImport = lan.RootElement.GetProperty("maLanImport").GetInt32();
            Assert.Equal(2, lan.RootElement.GetProperty("soThanhCong").GetInt32());
            Assert.Equal("hoan_tat", lan.RootElement.GetProperty("trangThai").GetString());
            Assert.Equal("cau-hoi.docx", lan.RootElement.GetProperty("tenFile").GetString());
        }

        // Câu hỏi giữ đúng mã trong tệp (STT 5 dù chưa có 1-4) và nối với lần nhập.
        using var danhSach = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/cau-hoi?sapXep=maCauHoi", _token));
        var ma = danhSach.RootElement.GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("maCauHoi").GetString()!).ToArray();
        Assert.Equal(["A1-5", "B2-1"], ma);
        Assert.Equal(2, await DemAsync($"SELECT COUNT(*) FROM dbo.CAUHOI WHERE MALANIMPORT = {maLanImport}"));

        var maCH = danhSach.RootElement.GetProperty("duLieu")[0].GetProperty("maCH").GetInt32();
        using var chiTiet = await DocAsync(await _client.GuiAsync(HttpMethod.Get, $"/api/cau-hoi/{maCH}", _token));
        Assert.Contains("{{media:NOIDUNG:1}}", chiTiet.RootElement.GetProperty("noiDung").GetString());
        Assert.Equal("B", chiTiet.RootElement.GetProperty("dapAnDung").GetString());
        var hinh = chiTiet.RootElement.GetProperty("hinhAnh")[0];
        Assert.Equal("NOIDUNG", hinh.GetProperty("viTri").GetString());

        var anh = await _client.GuiAsync(HttpMethod.Get, hinh.GetProperty("url").GetString()!, _token);
        Assert.Equal(HttpStatusCode.OK, anh.StatusCode);
        Assert.Equal("image/png", anh.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", anh.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("sandbox", anh.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal(DocxCauHoiMau.OnePixelPng, await anh.Content.ReadAsByteArrayAsync());

        // Lịch sử và chi tiết lần nhập; bản xem trước dùng một lần.
        using var lichSu = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/nhap-cau-hoi", _token));
        Assert.Equal(1, lichSu.RootElement.GetProperty("tongSoDong").GetInt32());
        Assert.Equal("Nguyễn Thảo", lichSu.RootElement.GetProperty("duLieu")[0].GetProperty("nguoiThucHien").GetString());
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GuiAsync(HttpMethod.Get, $"/api/nhap-cau-hoi/{maLanImport}", _token)).StatusCode);
        var lanHai = await XacNhanAsync(maXemTruoc);
        Assert.Equal(HttpStatusCode.NotFound, lanHai.StatusCode);
        Assert.Equal("IMPORT_PREVIEW_NOT_FOUND", await lanHai.MaLoiAsync());
        Assert.Empty(TepTam());
    }

    [Fact]
    public async Task Preview_reports_database_conflicts_and_gives_no_preview_id()
    {
        var a = await TaoKqhtAsync(1);
        var b = await TaoKqhtAsync(2);
        await _client.GuiAsync(HttpMethod.Patch, $"/api/kqht/{b}/trang-thai", _token, body: new { trangThai = "ngung_su_dung" });
        await TaoCauHoiAsync(a); // A1-1

        using var xemTruoc = await XemTruocAsync(
            new DongMau("A1-1"),
            new DongMau("B1-1"),
            new DongMau("C1-1"),
            new DongMau("A1-2", NoiDung: new string('x', 4001)),
            new DongMau("A1-3"));

        var root = xemTruoc.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("maXemTruoc").ValueKind);
        Assert.Equal(4, root.GetProperty("soDongLoi").GetInt32());
        var maLoi = root.GetProperty("dong").EnumerateArray()
            .Select(d => string.Join(",", d.GetProperty("vanDe").EnumerateArray().Select(v => v.GetProperty("code").GetString())))
            .ToArray();
        Assert.Equal(["DUPLICATE_IN_DATABASE", "KQHT_INACTIVE", "KQHT_NOT_FOUND", "CONTENT_TOO_LONG", ""], maLoi);
        Assert.Empty(TepTam());
    }

    [Fact]
    public async Task Confirm_rechecks_database_and_writes_nothing_when_a_code_was_taken_meanwhile()
    {
        var a = await TaoKqhtAsync(1);
        using var xemTruoc = await XemTruocAsync(new DongMau("A1-1", Hinh: ImagePartType.Png), new DongMau("A1-2"));
        var maXemTruoc = xemTruoc.RootElement.GetProperty("maXemTruoc").GetString()!;
        await TaoCauHoiAsync(a); // chiếm A1-1 sau khi xem trước

        var response = await XacNhanAsync(maXemTruoc);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("QUESTION_IMPORT_HAS_ERRORS", await response.MaLoiAsync());
        Assert.Equal(0, await DemAsync("SELECT COUNT(*) FROM dbo.LANIMPORT"));
        Assert.Equal(1, await DemAsync("SELECT COUNT(*) FROM dbo.CAUHOI"));
        Assert.False(Directory.Exists(Path.Combine(_factory.StorageRoot, "cau-hoi"))
                     && Directory.EnumerateFiles(Path.Combine(_factory.StorageRoot, "cau-hoi"), "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Expired_preview_returns_410_and_is_removed()
    {
        await TaoKqhtAsync(1);
        using var xemTruoc = await XemTruocAsync(new DongMau("A1-1"));
        var maXemTruoc = xemTruoc.RootElement.GetProperty("maXemTruoc").GetString()!;
        var meta = Path.Combine(_factory.StorageRoot, "tam", "nhap-cau-hoi", $"{maXemTruoc}.json");
        var json = await File.ReadAllTextAsync(meta);
        await File.WriteAllTextAsync(meta, json.Replace(
            JsonDocument.Parse(json).RootElement.GetProperty("HetHan").GetString()!,
            DateTime.UtcNow.AddMinutes(-1).ToString("O")));

        var response = await XacNhanAsync(maXemTruoc);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal("IMPORT_PREVIEW_EXPIRED", await response.MaLoiAsync());
        Assert.Empty(TepTam());
        Assert.Equal(0, await DemAsync("SELECT COUNT(*) FROM dbo.CAUHOI"));
    }

    [Theory]
    [InlineData("khong-hop-le")]
    [InlineData("..%2F..%2Fappsettings")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public async Task Unknown_or_malformed_preview_id_returns_404(string maXemTruoc)
    {
        var response = await XacNhanAsync(maXemTruoc);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Another_admin_cannot_confirm_someone_elses_preview()
    {
        await TaoKqhtAsync(1);
        using var xemTruoc = await XemTruocAsync(new DongMau("A1-1"));
        var maXemTruoc = xemTruoc.RootElement.GetProperty("maXemTruoc").GetString()!;
        await DuLieuXacThuc.TaoAdminAsync(database, "admin2");
        var token2 = (await _client.DangNhapAsync("admin2", DuLieuXacThuc.MatKhauAdmin)).AccessToken;

        var response = await _client.GuiAsync(HttpMethod.Post, $"/api/nhap-cau-hoi/xem-truoc/{maXemTruoc}/xac-nhan", token2);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await XacNhanAsync(maXemTruoc)).StatusCode);
    }

    [Fact]
    public async Task Deleting_imported_question_removes_its_image_file()
    {
        await TaoKqhtAsync(1);
        using var xemTruoc = await XemTruocAsync(new DongMau("A1-1", Hinh: ImagePartType.Png));
        await XacNhanAsync(xemTruoc.RootElement.GetProperty("maXemTruoc").GetString()!);
        var thuMucAnh = Path.Combine(_factory.StorageRoot, "cau-hoi");
        var soFileTruoc = Directory.EnumerateFiles(thuMucAnh, "*", SearchOption.AllDirectories).Count();
        using var danhSach = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/cau-hoi", _token));
        var maCH = danhSach.RootElement.GetProperty("duLieu")[0].GetProperty("maCH").GetInt32();

        var xoa = await _client.GuiAsync(HttpMethod.Delete, $"/api/cau-hoi/{maCH}", _token);

        Assert.Equal(HttpStatusCode.NoContent, xoa.StatusCode);
        Assert.Equal(soFileTruoc - 1, Directory.EnumerateFiles(thuMucAnh, "*", SearchOption.AllDirectories).Count());
    }

    [Fact]
    public async Task Image_of_another_question_is_not_served()
    {
        await TaoKqhtAsync(1);
        using var xemTruoc = await XemTruocAsync(new DongMau("A1-1", Hinh: ImagePartType.Png), new DongMau("A1-2"));
        await XacNhanAsync(xemTruoc.RootElement.GetProperty("maXemTruoc").GetString()!);
        var maTN = await DemAsync("SELECT MAX(MATN) FROM dbo.TAINGUYEN_CAUHOI");
        var maCHKhac = await DemAsync("SELECT MACH FROM dbo.CAUHOI WHERE MACAUHOI = 'A1-2'");

        var response = await _client.GuiAsync(HttpMethod.Get, $"/api/cau-hoi/{maCHKhac}/hinh-anh/{maTN}", _token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("QUESTION_MEDIA_NOT_FOUND", await response.MaLoiAsync());
    }

    // ---------- Hỗ trợ ----------

    private async Task<JsonDocument> XemTruocAsync(params DongMau[] dong)
    {
        using var docx = DocxCauHoiMau.Tao(dong);
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(docx.ToArray());
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(file, "file", "cau-hoi.docx");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/nhap-cau-hoi/xem-truoc") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await DocAsync(response);
    }

    private Task<HttpResponseMessage> XacNhanAsync(string maXemTruoc) =>
        _client.GuiAsync(HttpMethod.Post, $"/api/nhap-cau-hoi/xem-truoc/{maXemTruoc}/xac-nhan", _token);

    private async Task<int> TaoKqhtAsync(int thuTu)
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/kqht", _token,
            body: new { tenKQHT = $"KQHT {thuTu}", thuTu, kyHieu = ((char)('A' + thuTu - 1)).ToString() });
        using var body = await DocAsync(response);
        return body.RootElement.GetProperty("maKQHT").GetInt32();
    }

    private Task<HttpResponseMessage> TaoCauHoiAsync(int maKQHT) =>
        _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: new
        {
            maKQHT,
            maMD = 1,
            noiDung = "Câu thêm tay",
            phuongAnA = "A",
            phuongAnB = "B",
            phuongAnC = "C",
            phuongAnD = "D",
            dapAnDung = "A"
        });

    private string[] TepTam()
    {
        var thuMuc = Path.Combine(_factory.StorageRoot, "tam", "nhap-cau-hoi");
        return Directory.Exists(thuMuc) ? Directory.GetFiles(thuMuc) : [];
    }

    private async Task<int> DemAsync(string sql)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<JsonDocument> DocAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
