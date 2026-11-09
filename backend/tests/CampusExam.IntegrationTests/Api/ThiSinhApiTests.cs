using System.Net;
using System.Text.Json;
using CampusExam.IntegrationTests.Database;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CampusExam.IntegrationTests.Api;

/// <summary>Hồ sơ và tài khoản thí sinh qua HTTP (API_DESIGN.md mục 2.6, D-THISINH-01).</summary>
[Collection(SqlServerCollection.Name)]
public sealed class ThiSinhApiTests(SqlServerDatabaseFixture database) : IAsyncLifetime
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
    public async Task Create_makes_account_with_birthday_password_that_must_be_changed()
    {
        var response = await TaoAsync("SV100", ngaySinh: "2005-03-14", cccd: "012345678901");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/thi-sinh/SV100", response.Headers.Location?.AbsolutePath);
        using (var body = await DocAsync(response))
        {
            var root = body.RootElement;
            Assert.Equal("Nguyễn Văn An", root.GetProperty("hoTen").GetString());
            Assert.Equal("2005-03-14", root.GetProperty("ngaySinh").GetString());
            Assert.Equal("nam", root.GetProperty("gioiTinh").GetString());
            Assert.Equal("hoat_dong", root.GetProperty("trangThaiTaiKhoan").GetString());
            Assert.False(root.GetProperty("daDoiMatKhau").GetBoolean());
            Assert.Equal("012345678901", root.GetProperty("cccd").GetString());
            Assert.False(root.TryGetProperty("matKhau", out _));
        }

        var phien = await _client.DangNhapAsync("SV100", "14032005");
        Assert.Equal(HttpStatusCode.OK, phien.Response.StatusCode);
        Assert.True(phien.Body.RootElement.GetProperty("taiKhoan").GetProperty("phaiDoiMatKhau").GetBoolean());
        Assert.Equal("SV100", phien.Body.RootElement.GetProperty("taiKhoan").GetProperty("msts").GetString());
    }

    [Fact]
    public async Task Create_rejects_taken_msts_admin_username_and_cccd()
    {
        await TaoAsync("SV100", cccd: "012345678901");

        var trungMsts = await TaoAsync("SV100");
        var trungTenAdmin = await TaoAsync(DuLieuXacThuc.TenAdmin);
        var trungCccd = await TaoAsync("SV101", cccd: "012345678901");

        Assert.Equal("STUDENT_MSTS_CONFLICT", await trungMsts.MaLoiAsync());
        Assert.Equal("STUDENT_MSTS_CONFLICT", await trungTenAdmin.MaLoiAsync());
        Assert.Equal(HttpStatusCode.Conflict, trungCccd.StatusCode);
        Assert.Equal("STUDENT_CCCD_CONFLICT", await trungCccd.MaLoiAsync());
    }

    [Fact]
    public async Task Create_validates_fields()
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/thi-sinh", _token, body: new
        {
            msts = "SV 01",
            hoTen = " ",
            cccd = "12345"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await DocAsync(response);
        var errors = body.RootElement.GetProperty("errors");
        foreach (var truong in new[] { "msts", "hoTen", "ngaySinh", "cccd" })
        {
            Assert.True(errors.TryGetProperty(truong, out _), $"Thiếu lỗi cho {truong}");
        }
    }

    [Theory]
    [InlineData("2999-01-01")]
    [InlineData("1850-01-01")]
    public async Task Create_rejects_unrealistic_birthday(string ngaySinh)
    {
        var response = await TaoAsync("SV100", ngaySinh: ngaySinh);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await DocAsync(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("ngaySinh", out _));
    }

    [Fact]
    public async Task Gender_accepts_only_vietnamese_strings()
    {
        var response = await _client.GuiAsync(HttpMethod.Post, "/api/thi-sinh", _token, body: new
        {
            msts = "SV100",
            hoTen = "An",
            ngaySinh = "2005-01-01",
            gioiTinh = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_profile_and_account_name_but_not_password()
    {
        await TaoAsync("SV100", ngaySinh: "2005-03-14");

        var response = await _client.GuiAsync(HttpMethod.Put, "/api/thi-sinh/SV100", _token, body: new
        {
            hoTen = "Trần Thị Bình",
            ngaySinh = "2004-12-01",
            gioiTinh = "nu",
            lop = " K30 ",
            noiSinh = "  "
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using (var body = await DocAsync(response))
        {
            Assert.Equal("Trần Thị Bình", body.RootElement.GetProperty("hoTen").GetString());
            Assert.Equal("nu", body.RootElement.GetProperty("gioiTinh").GetString());
            Assert.Equal("K30", body.RootElement.GetProperty("lop").GetString());
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("noiSinh").ValueKind);
        }

        // D-THISINH-01: sửa ngày sinh không đổi mật khẩu.
        var phien = await _client.DangNhapAsync("SV100", "14032005");
        Assert.Equal(HttpStatusCode.OK, phien.Response.StatusCode);
        Assert.Equal("Trần Thị Bình", phien.Body.RootElement.GetProperty("taiKhoan").GetProperty("hoTen").GetString());
    }

    [Fact]
    public async Task Missing_student_returns_404_on_every_route()
    {
        var responses = new[]
        {
            await _client.GuiAsync(HttpMethod.Get, "/api/thi-sinh/KHONGCO", _token),
            await _client.GuiAsync(HttpMethod.Put, "/api/thi-sinh/KHONGCO", _token,
                body: new { hoTen = "A", ngaySinh = "2005-01-01" }),
            await _client.GuiAsync(HttpMethod.Patch, "/api/thi-sinh/KHONGCO/trang-thai-tai-khoan", _token,
                body: new { trangThai = "khoa" }),
            await _client.GuiAsync(HttpMethod.Post, "/api/thi-sinh/KHONGCO/cap-lai-mat-khau", _token),
            await _client.GuiAsync(HttpMethod.Delete, "/api/thi-sinh/KHONGCO", _token)
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("STUDENT_NOT_FOUND", await response.MaLoiAsync());
        }
    }

    [Fact]
    public async Task Lock_revokes_existing_token_immediately_and_unlock_restores_login()
    {
        await TaoAsync("SV100", ngaySinh: "2005-03-14");
        var phien = await _client.DangNhapAsync("SV100", "14032005");
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", phien.AccessToken)).StatusCode);

        var khoa = await _client.GuiAsync(HttpMethod.Patch, "/api/thi-sinh/SV100/trang-thai-tai-khoan", _token,
            body: new { trangThai = "khoa" });

        Assert.Equal(HttpStatusCode.OK, khoa.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", phien.AccessToken)).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await _client.DangNhapAsync("SV100", "14032005")).Response.StatusCode);
        var lamMoi = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi",
            refreshCookie: phien.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, lamMoi.StatusCode);

        var mo = await _client.GuiAsync(HttpMethod.Patch, "/api/thi-sinh/SV100/trang-thai-tai-khoan", _token,
            body: new { trangThai = "hoat_dong" });

        Assert.Equal(HttpStatusCode.OK, mo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.DangNhapAsync("SV100", "14032005")).Response.StatusCode);
        Assert.True(await DemNhatKyAsync("Khóa tài khoản") == 1 && await DemNhatKyAsync("Mở khóa tài khoản") == 1);
    }

    [Fact]
    public async Task Reset_password_restores_birthday_password_and_revokes_sessions()
    {
        await TaoAsync("SV100", ngaySinh: "2005-03-14");
        var phien = await _client.DangNhapAsync("SV100", "14032005");
        var doi = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", phien.AccessToken,
            body: new { matKhauCu = "14032005", matKhauMoi = "MatKhauMoi2026" });
        Assert.Equal(HttpStatusCode.OK, doi.StatusCode);
        var phienMoi = await HttpTestExtensions.DocPhienAsync(doi);

        var response = await _client.GuiAsync(HttpMethod.Post, "/api/thi-sinh/SV100/cap-lai-mat-khau", _token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using (var body = await DocAsync(response))
        {
            Assert.False(body.RootElement.GetProperty("daDoiMatKhau").GetBoolean());
        }

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", phienMoi.AccessToken)).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await _client.DangNhapAsync("SV100", "MatKhauMoi2026")).Response.StatusCode);
        var macDinh = await _client.DangNhapAsync("SV100", "14032005");
        Assert.Equal(HttpStatusCode.OK, macDinh.Response.StatusCode);
        Assert.True(macDinh.Body.RootElement.GetProperty("taiKhoan").GetProperty("phaiDoiMatKhau").GetBoolean());
        Assert.Equal(1, await DemNhatKyAsync("Cấp lại mật khẩu"));
    }

    [Fact]
    public async Task List_filters_pages_and_hides_cccd()
    {
        await TaoAsync("SV001", hoTen: "Nguyễn An", lop: "K29", cccd: "000000000001");
        await TaoAsync("SV002", hoTen: "Trần Bình", lop: "K29");
        await TaoAsync("SV003", hoTen: "Lê Chi", lop: "K30", ngaySinh: "2005-03-14");
        await _client.DangNhapAsync("SV003", "14032005");
        await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau",
            (await _client.DangNhapAsync("SV003", "14032005")).AccessToken,
            body: new { matKhauCu = "14032005", matKhauMoi = "MatKhauMoi2026" });
        await _client.GuiAsync(HttpMethod.Patch, "/api/thi-sinh/SV002/trang-thai-tai-khoan", _token,
            body: new { trangThai = "khoa" });

        Assert.Equal(["SV001", "SV002"], await MstsAsync("lop=K29"));
        Assert.Equal(["SV002"], await MstsAsync("trangThai=khoa"));
        Assert.Equal(["SV001", "SV002"], await MstsAsync("daDoiMatKhau=false"));
        Assert.Equal(["SV003"], await MstsAsync("tuKhoa=Chi"));
        Assert.Equal(["SV003", "SV002", "SV001"], await MstsAsync("sapXep=-msts"));

        using (var trang = await DocAsync(await _client.GuiAsync(HttpMethod.Get,
            "/api/thi-sinh?trang=2&kichThuocTrang=2", _token)))
        {
            Assert.Equal(3, trang.RootElement.GetProperty("tongSoDong").GetInt32());
            Assert.Equal(1, trang.RootElement.GetProperty("duLieu").GetArrayLength());
            Assert.False(trang.RootElement.GetProperty("duLieu")[0].TryGetProperty("cccd", out _));
        }

        using var lop = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/thi-sinh/lop", _token));
        Assert.Equal(["K29", "K30"], lop.RootElement.EnumerateArray().Select(x => x.GetString()!).ToArray());
    }

    [Theory]
    [InlineData("sapXep=cccd")]
    [InlineData("trangThai=1")]
    [InlineData("kichThuocTrang=0")]
    public async Task List_rejects_invalid_query(string query)
    {
        var response = await _client.GuiAsync(HttpMethod.Get, $"/api/thi-sinh?{query}", _token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_student_and_account_but_keeps_audit_log()
    {
        await TaoAsync("SV100", ngaySinh: "2005-03-14");
        await _client.DangNhapAsync("SV100", "14032005");

        var response = await _client.GuiAsync(HttpMethod.Delete, "/api/thi-sinh/SV100", _token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GuiAsync(HttpMethod.Get, "/api/thi-sinh/SV100", _token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DangNhapAsync("SV100", "14032005")).Response.StatusCode);
        // Nhật ký đăng nhập cũ của thí sinh vẫn còn, chỉ mất liên kết tài khoản.
        Assert.Equal(1, await DemAsync("SELECT COUNT(*) FROM dbo.NHATKYHD WHERE HANHDONG = N'Đăng nhập' AND MTK IS NULL"));
        Assert.Equal(1, await DemNhatKyAsync("Xóa thí sinh"));
        // Tạo lại được với cùng mã số.
        Assert.Equal(HttpStatusCode.Created, (await TaoAsync("SV100")).StatusCode);
    }

    [Fact]
    public async Task Delete_is_blocked_once_student_is_assigned_to_an_exam()
    {
        await TaoAsync("SV100");
        await ChayAsync("""
            DECLARE @mtk int = (SELECT MTK FROM dbo.TAIKHOAN WHERE VAITRO = 'ADMIN');
            INSERT dbo.MATRAN (TENMT, TONGSOCAU, THOIGIANLAM, TRANGTHAIMATRAN, NGAYTAO)
            VALUES (N'Ma trận', 1, 45, 1, SYSUTCDATETIME());
            INSERT dbo.DEGOC (MTK, MAMT, MADEGOCHIENTHI, TONGSOCAU, NGAYTAO)
            VALUES (@mtk, SCOPE_IDENTITY(), 'DE-1', 1, SYSUTCDATETIME());
            DECLARE @made int = SCOPE_IDENTITY();
            INSERT dbo.DECON (MADE, TENDECON, NGAYTAO) VALUES (@made, N'101', SYSUTCDATETIME());
            DECLARE @madecon int = SCOPE_IDENTITY();
            INSERT dbo.KYTHI (MTK, MADE, TENKT, MONTHI, TGBATDAU, THOILUONG, HIENDIEM, CACHPHANDE,
                TRANGTHAIKYTHI, NGAYTAO, NGAYPHATHANH, SOPHUTCHOVAOTRE, CHOXEMLAI)
            VALUES (@mtk, @made, N'Giữa kỳ', N'CSDL', SYSUTCDATETIME(), 45, 1, 0, 1, SYSUTCDATETIME(),
                SYSUTCDATETIME(), 10, 0);
            INSERT dbo.PHANCONGTHI (MAKT, MSTS, MADECON, MADE) VALUES (SCOPE_IDENTITY(), 'SV100', @madecon, @made);
            """);

        var response = await _client.GuiAsync(HttpMethod.Delete, "/api/thi-sinh/SV100", _token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("STUDENT_IN_USE", await response.MaLoiAsync());
        using var chiTiet = await DocAsync(await _client.GuiAsync(HttpMethod.Get, "/api/thi-sinh/SV100", _token));
        Assert.Equal(1, chiTiet.RootElement.GetProperty("soLanPhanCong").GetInt32());
    }

    [Fact]
    public async Task Candidate_cannot_manage_students()
    {
        await TaoAsync("SV100", ngaySinh: "2005-03-14");
        var phien = await _client.DangNhapAsync("SV100", "14032005");
        var doi = await HttpTestExtensions.DocPhienAsync(await _client.GuiAsync(HttpMethod.Post,
            "/api/xac-thuc/doi-mat-khau", phien.AccessToken,
            body: new { matKhauCu = "14032005", matKhauMoi = "MatKhauMoi2026" }));

        var response = await _client.GuiAsync(HttpMethod.Get, "/api/thi-sinh", doi.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Hỗ trợ ----------

    private Task<HttpResponseMessage> TaoAsync(
        string msts,
        string hoTen = "Nguyễn Văn An",
        string ngaySinh = "2005-01-01",
        string? lop = null,
        string? cccd = null) =>
        _client.GuiAsync(HttpMethod.Post, "/api/thi-sinh", _token,
            body: new { msts, hoTen, ngaySinh, gioiTinh = "nam", lop, cccd });

    private async Task<string[]> MstsAsync(string query)
    {
        using var body = await DocAsync(await _client.GuiAsync(HttpMethod.Get, $"/api/thi-sinh?{query}", _token));
        return body.RootElement.GetProperty("duLieu").EnumerateArray()
            .Select(x => x.GetProperty("msts").GetString()!).ToArray();
    }

    private Task<int> DemNhatKyAsync(string hanhDong) =>
        DemAsync($"SELECT COUNT(*) FROM dbo.NHATKYHD WHERE NHOM = 'THISINH' AND HANHDONG = N'{hanhDong}'");

    private async Task<int> DemAsync(string sql)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

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
