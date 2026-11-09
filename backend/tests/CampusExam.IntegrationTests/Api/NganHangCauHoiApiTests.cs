using System.Net;
using System.Text.Json;
using CampusExam.IntegrationTests.Database;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CampusExam.IntegrationTests.Api;

/// <summary>
/// Bước 3 của docs/API_DESIGN.md: sửa/ngừng dùng/xóa KQHT và CRUD câu hỏi qua pipeline HTTP thật,
/// theo các quyết định đã chốt ngày 09/11/2026 (D-KQHT-01 phương án A, chuyển trạng thái tự do).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class NganHangCauHoiApiTests(SqlServerDatabaseFixture database) : IAsyncLifetime
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

    // ---------- KQHT ----------

    [Fact]
    public async Task Kqht_list_reports_question_count()
    {
        var maKQHT = await TaoKqhtAsync(1);
        await TaoCauHoiAsync(maKQHT, maMD: 1);
        await TaoCauHoiAsync(maKQHT, maMD: 2);

        using var body = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/kqht", _token));

        Assert.Equal(2, body.RootElement[0].GetProperty("soCauHoi").GetInt32());
    }

    [Fact]
    public async Task Kqht_update_changes_name_and_derives_ky_hieu_from_thu_tu()
    {
        var maKQHT = await TaoKqhtAsync(1);

        var response = await _client.GuiAsync(HttpMethod.Put, $"/api/kqht/{maKQHT}", _token,
            body: new { tenKQHT = "  Phân tích  ", thuTu = 3, moTa = "  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await DocAsync(response);
        Assert.Equal("Phân tích", body.RootElement.GetProperty("tenKQHT").GetString());
        Assert.Equal(3, body.RootElement.GetProperty("thuTu").GetInt32());
        Assert.Equal("C", body.RootElement.GetProperty("kyHieu").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("moTa").ValueKind);
    }

    [Fact]
    public async Task Kqht_with_questions_cannot_change_thu_tu_but_can_rename()
    {
        var maKQHT = await TaoKqhtAsync(1);
        await TaoCauHoiAsync(maKQHT);

        var doiThuTu = await _client.GuiAsync(HttpMethod.Put, $"/api/kqht/{maKQHT}", _token,
            body: new { tenKQHT = "Nhớ", thuTu = 2 });
        var doiTen = await _client.GuiAsync(HttpMethod.Put, $"/api/kqht/{maKQHT}", _token,
            body: new { tenKQHT = "Ghi nhớ", thuTu = 1 });

        Assert.Equal(HttpStatusCode.Conflict, doiThuTu.StatusCode);
        Assert.Equal("KQHT_IN_USE", await doiThuTu.MaLoiAsync());
        Assert.Equal(HttpStatusCode.OK, doiTen.StatusCode);
    }

    [Fact]
    public async Task Kqht_update_to_taken_thu_tu_returns_ky_hieu_conflict()
    {
        await TaoKqhtAsync(1);
        var maKQHT = await TaoKqhtAsync(2);

        var response = await _client.GuiAsync(HttpMethod.Put, $"/api/kqht/{maKQHT}", _token,
            body: new { tenKQHT = "Hiểu", thuTu = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("KQHT_KY_HIEU_CONFLICT", await response.MaLoiAsync());
    }

    [Fact]
    public async Task Kqht_update_missing_returns_not_found()
    {
        var response = await _client.GuiAsync(HttpMethod.Put, "/api/kqht/999999", _token,
            body: new { tenKQHT = "Nhớ", thuTu = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("KQHT_NOT_FOUND", await response.MaLoiAsync());
    }

    [Fact]
    public async Task Kqht_status_toggles_both_ways_and_requires_value()
    {
        var maKQHT = await TaoKqhtAsync(1);

        var ngung = await _client.GuiAsync(HttpMethod.Patch, $"/api/kqht/{maKQHT}/trang-thai", _token,
            body: new { trangThai = "ngung_su_dung" });
        var khoiPhuc = await _client.GuiAsync(HttpMethod.Patch, $"/api/kqht/{maKQHT}/trang-thai", _token,
            body: new { trangThai = "dang_su_dung" });
        var thieu = await _client.GuiAsync(HttpMethod.Patch, $"/api/kqht/{maKQHT}/trang-thai", _token,
            body: new { });

        using (var body = await DocAsync(ngung))
        {
            Assert.Equal("ngung_su_dung", body.RootElement.GetProperty("trangThai").GetString());
        }

        using (var body = await DocAsync(khoiPhuc))
        {
            Assert.Equal("dang_su_dung", body.RootElement.GetProperty("trangThai").GetString());
        }

        Assert.Equal(HttpStatusCode.BadRequest, thieu.StatusCode);
    }

    [Fact]
    public async Task Kqht_delete_only_when_unreferenced()
    {
        var trong = await TaoKqhtAsync(1);
        var coCauHoi = await TaoKqhtAsync(2);
        await TaoCauHoiAsync(coCauHoi);

        var xoaTrong = await _client.GuiAsync(HttpMethod.Delete, $"/api/kqht/{trong}", _token);
        var xoaCoCauHoi = await _client.GuiAsync(HttpMethod.Delete, $"/api/kqht/{coCauHoi}", _token);
        var xoaLai = await _client.GuiAsync(HttpMethod.Delete, $"/api/kqht/{trong}", _token);

        Assert.Equal(HttpStatusCode.NoContent, xoaTrong.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, xoaCoCauHoi.StatusCode);
        Assert.Equal("KQHT_IN_USE", await xoaCoCauHoi.MaLoiAsync());
        Assert.Equal(HttpStatusCode.NotFound, xoaLai.StatusCode);
    }

    [Fact]
    public async Task Kqht_used_by_matrix_cannot_be_deleted()
    {
        var maKQHT = await TaoKqhtAsync(1);
        await ChayAsync($"""
            INSERT dbo.MATRAN (TENMT, TONGSOCAU, THOIGIANLAM, TRANGTHAIMATRAN, NGAYTAO)
            VALUES (N'Ma trận thử', 10, 45, 0, SYSUTCDATETIME());
            INSERT dbo.CAU_TRUC (MAMD, MAMT, MAKQHT, SOCAU) VALUES (1, SCOPE_IDENTITY(), {maKQHT}, 10);
            """);

        var response = await _client.GuiAsync(HttpMethod.Delete, $"/api/kqht/{maKQHT}", _token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("KQHT_IN_USE", await response.MaLoiAsync());
    }

    // ---------- Câu hỏi ----------

    [Fact]
    public async Task Create_question_assigns_next_stt_per_kqht_and_level()
    {
        var a = await TaoKqhtAsync(1);
        var b = await TaoKqhtAsync(2);

        var ma1 = await TaoCauHoiLayMaAsync(a, 1);
        var ma2 = await TaoCauHoiLayMaAsync(a, 1);
        var ma3 = await TaoCauHoiLayMaAsync(a, 2);
        var ma4 = await TaoCauHoiLayMaAsync(b, 1);

        Assert.Equal(["A1-1", "A1-2", "A2-1", "B1-1"], new[] { ma1, ma2, ma3, ma4 });
    }

    [Fact]
    public async Task Concurrent_creates_get_distinct_codes()
    {
        var maKQHT = await TaoKqhtAsync(1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: CauHoiMoi(maKQHT, 1))));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var maCauHoi = new List<string>();
        foreach (var response in responses)
        {
            using var body = await DocAsync(response);
            maCauHoi.Add(body.RootElement.GetProperty("maCauHoi").GetString()!);
        }

        Assert.Equal(Enumerable.Range(1, 8).Select(stt => $"A1-{stt}").Order(), maCauHoi.Order());
    }

    [Fact]
    public async Task Create_question_returns_detail_with_location_and_normalized_answer()
    {
        var maKQHT = await TaoKqhtAsync(1);

        var response = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: new
        {
            maKQHT,
            maMD = 2,
            noiDung = "  Khóa chính là gì?  ",
            phuongAnA = "A",
            phuongAnB = "B",
            phuongAnC = "C",
            phuongAnD = "D",
            dapAnDung = "c",
            coDinhDapAn = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await DocAsync(response);
        var root = body.RootElement;
        Assert.Equal($"/api/cau-hoi/{root.GetProperty("maCH").GetInt32()}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("A2-1", root.GetProperty("maCauHoi").GetString());
        Assert.Equal("Khóa chính là gì?", root.GetProperty("noiDung").GetString());
        Assert.Equal("C", root.GetProperty("dapAnDung").GetString());
        Assert.True(root.GetProperty("coDinhDapAn").GetBoolean());
        Assert.Equal("dang_su_dung", root.GetProperty("trangThai").GetString());
        Assert.Equal(0, root.GetProperty("soDeDaDung").GetInt32());
        Assert.Equal(0, root.GetProperty("hinhAnh").GetArrayLength());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("phienBan").GetString()));
    }

    [Fact]
    public async Task Create_question_validates_fields()
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: new
        {
            maKQHT = 0,
            maMD = 4,
            noiDung = " ",
            phuongAnA = "A",
            phuongAnB = "B",
            phuongAnC = "C",
            phuongAnD = "D",
            dapAnDung = "E"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await DocAsync(response);
        var errors = body.RootElement.GetProperty("errors");
        foreach (var truong in new[] { "maKQHT", "maMD", "noiDung", "dapAnDung" })
        {
            Assert.True(errors.TryGetProperty(truong, out _), $"Thiếu lỗi cho {truong}");
        }
    }

    [Fact]
    public async Task Create_question_rejects_missing_or_inactive_kqht()
    {
        var maKQHT = await TaoKqhtAsync(1);
        await _client.GuiAsync(HttpMethod.Patch, $"/api/kqht/{maKQHT}/trang-thai", _token,
            body: new { trangThai = "ngung_su_dung" });

        var khongTonTai = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: CauHoiMoi(999999, 1));
        var ngungDung = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: CauHoiMoi(maKQHT, 1));

        Assert.Equal(HttpStatusCode.BadRequest, khongTonTai.StatusCode);
        using (var body = await DocAsync(khongTonTai))
        {
            Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("maKQHT", out _));
        }

        Assert.Equal(HttpStatusCode.Conflict, ngungDung.StatusCode);
        Assert.Equal("KQHT_INACTIVE", await ngungDung.MaLoiAsync());
    }

    [Fact]
    public async Task Update_question_requires_current_version()
    {
        var maKQHT = await TaoKqhtAsync(1);
        var (maCH, phienBan) = await TaoCauHoiAsync(maKQHT);

        var lan1 = await _client.GuiAsync(HttpMethod.Put, $"/api/cau-hoi/{maCH}", _token,
            body: NoiDungSua("Nội dung mới", phienBan));
        var lan2Cu = await _client.GuiAsync(HttpMethod.Put, $"/api/cau-hoi/{maCH}", _token,
            body: NoiDungSua("Ghi đè", phienBan));

        Assert.Equal(HttpStatusCode.OK, lan1.StatusCode);
        using (var body = await DocAsync(lan1))
        {
            Assert.Equal("Nội dung mới", body.RootElement.GetProperty("noiDung").GetString());
            Assert.NotEqual(phienBan, body.RootElement.GetProperty("phienBan").GetString());
            Assert.Equal(JsonValueKind.String, body.RootElement.GetProperty("ngayCapNhat").ValueKind);
        }

        Assert.Equal(HttpStatusCode.Conflict, lan2Cu.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await lan2Cu.MaLoiAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-phai-base64")]
    [InlineData("AAAA")]
    public async Task Update_question_rejects_invalid_version(string phienBan)
    {
        var maKQHT = await TaoKqhtAsync(1);
        var (maCH, _) = await TaoCauHoiAsync(maKQHT);

        var response = await _client.GuiAsync(HttpMethod.Put, $"/api/cau-hoi/{maCH}", _token,
            body: NoiDungSua("Mới", phienBan));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await DocAsync(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("phienBan", out _));
    }

    [Fact]
    public async Task Question_status_toggles_freely_and_missing_returns_404()
    {
        var maKQHT = await TaoKqhtAsync(1);
        var (maCH, _) = await TaoCauHoiAsync(maKQHT);

        var ngung = await _client.GuiAsync(HttpMethod.Patch, $"/api/cau-hoi/{maCH}/trang-thai", _token,
            body: new { trangThai = "ngung_su_dung" });
        var khoiPhuc = await _client.GuiAsync(HttpMethod.Patch, $"/api/cau-hoi/{maCH}/trang-thai", _token,
            body: new { trangThai = "dang_su_dung" });
        var khongCo = await _client.GuiAsync(HttpMethod.Patch, "/api/cau-hoi/999999/trang-thai", _token,
            body: new { trangThai = "dang_su_dung" });

        using (var body = await DocAsync(ngung))
        {
            Assert.Equal("ngung_su_dung", body.RootElement.GetProperty("trangThai").GetString());
        }

        using (var body = await DocAsync(khoiPhuc))
        {
            Assert.Equal("dang_su_dung", body.RootElement.GetProperty("trangThai").GetString());
        }

        Assert.Equal(HttpStatusCode.NotFound, khongCo.StatusCode);
        Assert.Equal("QUESTION_NOT_FOUND", await khongCo.MaLoiAsync());
    }

    [Fact]
    public async Task Delete_question_only_when_not_in_any_exam()
    {
        var maKQHT = await TaoKqhtAsync(1);
        var (tuDo, _) = await TaoCauHoiAsync(maKQHT);
        var (trongDe, _) = await TaoCauHoiAsync(maKQHT);
        await DuaVaoDeAsync(trongDe);

        var xoaTuDo = await _client.GuiAsync(HttpMethod.Delete, $"/api/cau-hoi/{tuDo}", _token);
        var xoaTrongDe = await _client.GuiAsync(HttpMethod.Delete, $"/api/cau-hoi/{trongDe}", _token);
        var chiTiet = await _client.GuiAsync(HttpMethod.Get, $"/api/cau-hoi/{tuDo}", _token);

        Assert.Equal(HttpStatusCode.NoContent, xoaTuDo.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, xoaTrongDe.StatusCode);
        Assert.Equal("QUESTION_IN_USE", await xoaTrongDe.MaLoiAsync());
        Assert.Equal(HttpStatusCode.NotFound, chiTiet.StatusCode);

        using var body = await DocAsync(await _client.GuiAsync(HttpMethod.Get, $"/api/cau-hoi/{trongDe}", _token));
        Assert.Equal(1, body.RootElement.GetProperty("soDeDaDung").GetInt32());
    }

    [Fact]
    public async Task List_filters_sorts_pages_and_omits_answers()
    {
        var a = await TaoKqhtAsync(1);
        var b = await TaoKqhtAsync(2);
        for (var i = 0; i < 11; i++)
        {
            await TaoCauHoiAsync(a, maMD: 1, noiDung: $"Câu chuẩn hóa {i}");
        }

        await TaoCauHoiAsync(a, maMD: 2, noiDung: "Câu khác");
        var (ngung, _) = await TaoCauHoiAsync(b, maMD: 1, noiDung: new string('x', 300));
        await _client.GuiAsync(HttpMethod.Patch, $"/api/cau-hoi/{ngung}/trang-thai", _token,
            body: new { trangThai = "ngung_su_dung" });

        using (var trang2 = await DocAsync(await _client.GuiAsync(HttpMethod.Get,
            $"/api/cau-hoi?maKQHT={a}&maMD=1&sapXep=maCauHoi&trang=2&kichThuocTrang=5", _token)))
        {
            var root = trang2.RootElement;
            Assert.Equal(11, root.GetProperty("tongSoDong").GetInt32());
            Assert.Equal(3, root.GetProperty("tongSoTrang").GetInt32());
            var ma = root.GetProperty("duLieu").EnumerateArray()
                .Select(x => x.GetProperty("maCauHoi").GetString()!).ToArray();
            // Sắp theo số STT, không theo chuỗi: A1-6 ... A1-10.
            Assert.Equal(["A1-6", "A1-7", "A1-8", "A1-9", "A1-10"], ma);
            var dong = root.GetProperty("duLieu")[0];
            Assert.False(dong.TryGetProperty("dapAnDung", out _));
            Assert.Equal("A", dong.GetProperty("kqht").GetProperty("kyHieu").GetString());
        }

        using (var timKiem = await DocAsync(await _client.GuiAsync(HttpMethod.Get,
            "/api/cau-hoi?tuKhoa=khác", _token)))
        {
            Assert.Equal(1, timKiem.RootElement.GetProperty("tongSoDong").GetInt32());
        }

        using (var theoTrangThai = await DocAsync(await _client.GuiAsync(HttpMethod.Get,
            "/api/cau-hoi?trangThai=ngung_su_dung", _token)))
        {
            var dong = theoTrangThai.RootElement.GetProperty("duLieu")[0];
            Assert.Equal(1, theoTrangThai.RootElement.GetProperty("tongSoDong").GetInt32());
            Assert.Equal(EfTomTat, dong.GetProperty("tomTat").GetString()!.Length);
            Assert.EndsWith("…", dong.GetProperty("tomTat").GetString());
        }

        using var thongKe = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/cau-hoi/thong-ke", _token));
        Assert.Equal(13, thongKe.RootElement.GetProperty("tongSo").GetInt32());
        Assert.Equal(12, thongKe.RootElement.GetProperty("dangSuDung").GetInt32());
        Assert.Equal(1, thongKe.RootElement.GetProperty("ngungSuDung").GetInt32());
        Assert.Equal(2, thongKe.RootElement.GetProperty("soKQHT").GetInt32());
    }

    [Theory]
    [InlineData("sapXep=noiDung")]
    [InlineData("trangThai=1")]
    [InlineData("kichThuocTrang=101")]
    [InlineData("maMD=5")]
    public async Task List_rejects_invalid_query(string query)
    {
        var response = await _client.GuiAsync(HttpMethod.Get, $"/api/cau-hoi?{query}", _token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await response.MaLoiAsync());
    }

    [Fact]
    public async Task Candidate_cannot_reach_question_bank()
    {
        await DuLieuXacThuc.TaoThiSinhAsync(database);
        var phien = await _client.DangNhapAsync(DuLieuXacThuc.Msts, DuLieuXacThuc.MatKhauMacDinh);

        var response = await _client.GuiAsync(HttpMethod.Get, "/api/cau-hoi", phien.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Hỗ trợ ----------

    /// <summary>160 ký tự nội dung + dấu "…".</summary>
    private const int EfTomTat = 161;

    private async Task<int> TaoKqhtAsync(int thuTu)
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/kqht", _token,
            body: new { tenKQHT = $"KQHT {thuTu}", thuTu, kyHieu = ((char)('A' + thuTu - 1)).ToString() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await DocAsync(response);
        return body.RootElement.GetProperty("maKQHT").GetInt32();
    }

    private static object CauHoiMoi(int maKQHT, int maMD, string noiDung = "Câu hỏi thử") => new
    {
        maKQHT,
        maMD,
        noiDung,
        phuongAnA = "Phương án A",
        phuongAnB = "Phương án B",
        phuongAnC = "Phương án C",
        phuongAnD = "Phương án D",
        dapAnDung = "A"
    };

    private static object NoiDungSua(string noiDung, string phienBan) => new
    {
        noiDung,
        phuongAnA = "A",
        phuongAnB = "B",
        phuongAnC = "C",
        phuongAnD = "D",
        dapAnDung = "B",
        phienBan
    };

    private async Task<(int MaCH, string PhienBan)> TaoCauHoiAsync(int maKQHT, int maMD = 1, string noiDung = "Câu hỏi thử")
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: CauHoiMoi(maKQHT, maMD, noiDung));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await DocAsync(response);
        return (body.RootElement.GetProperty("maCH").GetInt32(), body.RootElement.GetProperty("phienBan").GetString()!);
    }

    private async Task<string> TaoCauHoiLayMaAsync(int maKQHT, int maMD)
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/cau-hoi", _token, body: CauHoiMoi(maKQHT, maMD));
        using var body = await DocAsync(response);
        return body.RootElement.GetProperty("maCauHoi").GetString()!;
    }

    /// <summary>Tạo ma trận + đề gốc tối thiểu và đưa câu hỏi vào GOM_CAU.</summary>
    private Task DuaVaoDeAsync(int maCH) => ChayAsync($"""
        DECLARE @mtk int = (SELECT TOP 1 MTK FROM dbo.TAIKHOAN);
        INSERT dbo.MATRAN (TENMT, TONGSOCAU, THOIGIANLAM, TRANGTHAIMATRAN, NGAYTAO)
        VALUES (N'Ma trận thử', 1, 45, 1, SYSUTCDATETIME());
        INSERT dbo.DEGOC (MTK, MAMT, MADEGOCHIENTHI, TONGSOCAU, NGAYTAO)
        VALUES (@mtk, SCOPE_IDENTITY(), 'DE-{maCH}', 1, SYSUTCDATETIME());
        INSERT dbo.GOM_CAU (MADE, MACH, THUTUTRONGDEGOC, NOIDUNGTAILUCTAO, PHUONGANATAILUCTAO,
            PHUONGANBTAILUCTAO, PHUONGANCTAILUCTAO, PHUONGANDTAILUCTAO, DAPANDUNGGOC)
        SELECT SCOPE_IDENTITY(), MACH, 1, NOIDUNG, DAA, DAB, DAC, DAD, DAPANDUNG
        FROM dbo.CAUHOI WHERE MACH = {maCH};
        """);

    private async Task ChayAsync(string sql)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<JsonDocument> DocAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
