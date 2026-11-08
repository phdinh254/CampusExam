using System.Net;
using System.Net.Http.Json;
using CampusExam.IntegrationTests.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusExam.IntegrationTests.Api;

[Collection(SqlServerCollection.Name)]
public sealed class XacThucApiTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    private readonly HttpClient _client = CampusExamApiFactory.For(database).TaoClient();

    public async Task InitializeAsync()
    {
        await database.ResetAsync();
        await DuLieuXacThuc.TaoAdminAsync(database);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Admin_dang_nhap_nhan_access_token_va_cookie_refresh_an_toan()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        Assert.Equal(HttpStatusCode.OK, phien.Response.StatusCode);
        var body = phien.Body.RootElement;
        Assert.False(string.IsNullOrEmpty(phien.AccessToken));
        Assert.True(body.GetProperty("hetHan").GetDateTime() > DateTime.UtcNow);
        var taiKhoan = body.GetProperty("taiKhoan");
        Assert.Equal("admin", taiKhoan.GetProperty("tenDN").GetString());
        Assert.Equal("Nguyễn Thảo", taiKhoan.GetProperty("hoTen").GetString());
        Assert.Equal("ADMIN", taiKhoan.GetProperty("vaiTro").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, taiKhoan.GetProperty("msts").ValueKind);
        Assert.False(taiKhoan.GetProperty("phaiDoiMatKhau").GetBoolean());
        Assert.False(body.TryGetProperty("refreshToken", out _));

        var cookie = phien.SetCookieHeader!.ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/xac-thuc", cookie);
        Assert.DoesNotContain("expires", cookie); // không ghi nhớ ⇒ cookie phiên

        var kqht = await _client.GuiAsync(HttpMethod.Get, "/api/kqht", phien.AccessToken);
        Assert.Equal(HttpStatusCode.OK, kqht.StatusCode);
    }

    [Fact]
    public async Task Ghi_nho_dang_nhap_dat_han_cookie()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin, ghiNho: true);

        Assert.Contains("expires", phien.SetCookieHeader!.ToLowerInvariant());
    }

    [Fact]
    public async Task Sai_mat_khau_va_ten_khong_ton_tai_tra_cung_mot_loi()
    {
        var saiMatKhau = await _client.DangNhapAsync("admin", "SaiMatKhau1");
        var khongTonTai = await _client.DangNhapAsync("khongco", "SaiMatKhau1");

        foreach (var phien in new[] { saiMatKhau, khongTonTai })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, phien.Response.StatusCode);
            Assert.Equal("AUTH_INVALID_CREDENTIALS", phien.Body.RootElement.GetProperty("code").GetString());
            Assert.Null(phien.RefreshCookie);
        }

        Assert.Equal(
            saiMatKhau.Body.RootElement.GetProperty("title").GetString(),
            khongTonTai.Body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Sai_5_lan_bi_tam_khoa_ke_ca_khi_nhap_dung_mat_khau()
    {
        for (var lan = 1; lan <= 4; lan++)
        {
            var sai = await _client.DangNhapAsync("admin", "SaiMatKhau1");
            Assert.Equal(HttpStatusCode.Unauthorized, sai.Response.StatusCode);
        }

        var lanThuNam = await _client.DangNhapAsync("admin", "SaiMatKhau1");
        Assert.Equal((HttpStatusCode)423, lanThuNam.Response.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_LOCKED", lanThuNam.Body.RootElement.GetProperty("code").GetString());
        Assert.True(lanThuNam.Body.RootElement.GetProperty("khoaDen").GetDateTime() > DateTime.UtcNow.AddMinutes(14));

        var dung = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);
        Assert.Equal((HttpStatusCode)423, dung.Response.StatusCode);
    }

    [Fact]
    public async Task Tam_khoa_chi_ap_dung_cho_ip_da_nhap_sai()
    {
        for (var lan = 1; lan <= 5; lan++)
        {
            await _client.DangNhapAsync("admin", "SaiMatKhau1", ip: "10.0.0.66");
        }

        var keTanCong = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin, ip: "10.0.0.66");
        Assert.Equal((HttpStatusCode)423, keTanCong.Response.StatusCode);

        // Chủ tài khoản ở máy khác vẫn đăng nhập được.
        var chuTaiKhoan = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin, ip: "10.0.0.7");
        Assert.Equal(HttpStatusCode.OK, chuTaiKhoan.Response.StatusCode);
    }

    [Fact]
    public async Task Ten_dang_nhap_khong_ton_tai_cung_bi_tam_khoa_nhu_ten_co_that()
    {
        PhienTest? lanThuNam = null;
        for (var lan = 1; lan <= 5; lan++)
        {
            lanThuNam = await _client.DangNhapAsync("khongco", "SaiMatKhau1");
        }

        // 423 không còn tiết lộ tên đăng nhập có tồn tại hay không.
        Assert.Equal((HttpStatusCode)423, lanThuNam!.Response.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_LOCKED", lanThuNam.Body.RootElement.GetProperty("code").GetString());
        Assert.True(lanThuNam.Body.RootElement.GetProperty("khoaDen").GetDateTime() > DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task Sai_lau_hon_thoi_gian_khoa_thi_dem_lai_tu_dau()
    {
        for (var lan = 1; lan <= 4; lan++)
        {
            await _client.DangNhapAsync("admin", "SaiMatKhau1");
        }

        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand(
                "UPDATE dbo.KHOADANGNHAP SET LANSAICUOI = DATEADD(MINUTE, -16, LANSAICUOI);", connection);
            await command.ExecuteNonQueryAsync();
        }

        var sai = await _client.DangNhapAsync("admin", "SaiMatKhau1");
        Assert.Equal(HttpStatusCode.Unauthorized, sai.Response.StatusCode);
    }

    [Fact]
    public async Task Dang_nhap_dung_dat_lai_so_lan_sai()
    {
        await _client.DangNhapAsync("admin", "SaiMatKhau1");
        await _client.DangNhapAsync("admin", "SaiMatKhau1");
        await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        await using var db = database.CreateDbContext();
        Assert.False(await db.KhoaDangNhaps.AnyAsync());
        var taiKhoan = await db.TaiKhoans.SingleAsync(x => x.TenDN == "admin");
        Assert.NotNull(taiKhoan.LanDNCuoi);
    }

    [Fact]
    public async Task Sai_mat_khau_cu_khi_doi_mat_khau_dem_chung_voi_dang_nhap()
    {
        var token = await _client.LayTokenAdminAsync();
        HttpResponseMessage? lanThuNam = null;
        for (var lan = 1; lan <= 5; lan++)
        {
            lanThuNam = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", token,
                body: new { matKhauCu = $"DoMatKhau{lan}", matKhauMoi = "MatKhauMoi1" });
        }

        Assert.Equal((HttpStatusCode)423, lanThuNam!.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_LOCKED", await lanThuNam.MaLoiAsync());

        // Đang khóa: nhập đúng mật khẩu cũ cũng bị từ chối, đăng nhập từ IP này cũng vậy.
        var dungMatKhau = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", token,
            body: new { matKhauCu = DuLieuXacThuc.MatKhauAdmin, matKhauMoi = "MatKhauMoi1" });
        Assert.Equal((HttpStatusCode)423, dungMatKhau.StatusCode);
        var dangNhap = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);
        Assert.Equal((HttpStatusCode)423, dangNhap.Response.StatusCode);
    }

    [Fact]
    public async Task Tai_khoan_bi_khoa_khong_dang_nhap_duoc()
    {
        await DuLieuXacThuc.TaoAdminAsync(database, "admin2", trangThai: 0);

        var phien = await _client.DangNhapAsync("admin2", DuLieuXacThuc.MatKhauAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, phien.Response.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_DISABLED", phien.Body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Goi_api_khong_co_token_hoac_token_hong_tra_401_co_ma_loi()
    {
        var khongToken = await _client.GuiAsync(HttpMethod.Get, "/api/kqht");
        Assert.Equal(HttpStatusCode.Unauthorized, khongToken.StatusCode);
        Assert.Equal("AUTH_UNAUTHENTICATED", await khongToken.MaLoiAsync());

        var tokenHong = await _client.GuiAsync(HttpMethod.Get, "/api/kqht", "abc.def.ghi");
        Assert.Equal(HttpStatusCode.Unauthorized, tokenHong.StatusCode);
        Assert.Equal("AUTH_TOKEN_EXPIRED", await tokenHong.MaLoiAsync());
    }

    [Fact]
    public async Task Health_va_openapi_khong_can_dang_nhap()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task Thi_sinh_dang_nhap_bang_ngay_sinh_va_bi_chan_cho_toi_khi_doi_mat_khau()
    {
        await DuLieuXacThuc.TaoThiSinhAsync(database);

        var phien = await _client.DangNhapAsync(DuLieuXacThuc.Msts, "14032005");

        Assert.Equal(HttpStatusCode.OK, phien.Response.StatusCode);
        var taiKhoan = phien.Body.RootElement.GetProperty("taiKhoan");
        Assert.Equal("THISINH", taiKhoan.GetProperty("vaiTro").GetString());
        Assert.Equal("SV001", taiKhoan.GetProperty("msts").GetString());
        Assert.True(taiKhoan.GetProperty("phaiDoiMatKhau").GetBoolean());

        var toi = await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", phien.AccessToken);
        Assert.Equal(HttpStatusCode.OK, toi.StatusCode);

        var api = await _client.GuiAsync(HttpMethod.Get, "/api/kqht", phien.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, api.StatusCode);
        Assert.Equal("AUTH_PASSWORD_CHANGE_REQUIRED", await api.MaLoiAsync());
    }

    [Fact]
    public async Task Doi_mat_khau_kiem_tra_chinh_sach_va_mat_khau_cu()
    {
        await DuLieuXacThuc.TaoThiSinhAsync(database);
        var phien = await _client.DangNhapAsync(DuLieuXacThuc.Msts, DuLieuXacThuc.MatKhauMacDinh);

        var saiCu = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", phien.AccessToken,
            body: new { matKhauCu = "SaiCu12345", matKhauMoi = "ThiSinh2026" });
        Assert.Equal(HttpStatusCode.BadRequest, saiCu.StatusCode);
        Assert.Equal("AUTH_CURRENT_PASSWORD_INCORRECT", await saiCu.MaLoiAsync());

        var chuaNgaySinh = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", phien.AccessToken,
            body: new { matKhauCu = DuLieuXacThuc.MatKhauMacDinh, matKhauMoi = "a14032005" });
        Assert.Equal(HttpStatusCode.BadRequest, chuaNgaySinh.StatusCode);
        using var loi = System.Text.Json.JsonDocument.Parse(await chuaNgaySinh.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_FAILED", loi.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            loi.RootElement.GetProperty("errors").GetProperty("matKhauMoi").EnumerateArray(),
            message => message.GetString() == "Mật khẩu không được chứa ngày sinh.");
    }

    [Fact]
    public async Task Doi_mat_khau_thanh_cong_vo_hieu_token_cu_va_cap_phien_moi()
    {
        await DuLieuXacThuc.TaoThiSinhAsync(database);
        var cu = await _client.DangNhapAsync(DuLieuXacThuc.Msts, DuLieuXacThuc.MatKhauMacDinh);

        var response = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", cu.AccessToken,
            body: new { matKhauCu = DuLieuXacThuc.MatKhauMacDinh, matKhauMoi = "ThiSinh2026" });
        var moi = await HttpTestExtensions.DocPhienAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(moi.Body.RootElement.GetProperty("taiKhoan").GetProperty("phaiDoiMatKhau").GetBoolean());
        Assert.NotNull(moi.RefreshCookie);

        // Token và phiên cũ hết hiệu lực.
        var tokenCu = await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", cu.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, tokenCu.StatusCode);
        var lamMoiCu = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: cu.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, lamMoiCu.StatusCode);

        // Thí sinh hết bị chặn đổi mật khẩu nhưng vẫn không có quyền quản trị.
        var api = await _client.GuiAsync(HttpMethod.Get, "/api/kqht", moi.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, api.StatusCode);
        Assert.Equal("FORBIDDEN", await api.MaLoiAsync());

        var dangNhapLai = await _client.DangNhapAsync(DuLieuXacThuc.Msts, "ThiSinh2026");
        Assert.Equal(HttpStatusCode.OK, dangNhapLai.Response.StatusCode);
        var matKhauCu = await _client.DangNhapAsync(DuLieuXacThuc.Msts, DuLieuXacThuc.MatKhauMacDinh);
        Assert.Equal(HttpStatusCode.Unauthorized, matKhauCu.Response.StatusCode);
    }

    [Fact]
    public async Task Lam_moi_bat_buoc_header_csrf_va_origin_hop_le()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        var thieuHeader = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie);
        Assert.Equal(HttpStatusCode.Forbidden, thieuHeader.StatusCode);
        Assert.Equal("AUTH_CSRF_FAILED", await thieuHeader.MaLoiAsync());

        var originLa = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi",
            refreshCookie: phien.RefreshCookie, csrf: true, origin: "https://evil.example");
        Assert.Equal(HttpStatusCode.Forbidden, originLa.StatusCode);

        var hopLe = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi",
            refreshCookie: phien.RefreshCookie, csrf: true, origin: CampusExamApiFactory.AllowedOrigin);
        Assert.Equal(HttpStatusCode.OK, hopLe.StatusCode);
    }

    [Fact]
    public async Task Lam_moi_xoay_vong_refresh_token()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        var lan1 = await HttpTestExtensions.DocPhienAsync(await _client.GuiAsync(
            HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie, csrf: true));

        Assert.Equal(HttpStatusCode.OK, lan1.Response.StatusCode);
        Assert.NotNull(lan1.RefreshCookie);
        Assert.NotEqual(phien.RefreshCookie, lan1.RefreshCookie);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", lan1.AccessToken)).StatusCode);

        // Token cũ dùng lại ngay (ví dụ tab khác): bị từ chối nhưng phiên mới vẫn còn.
        var dungLai = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, dungLai.StatusCode);
        Assert.Equal("AUTH_REFRESH_INVALID", await dungLai.MaLoiAsync());

        var lan2 = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: lan1.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.OK, lan2.StatusCode);
    }

    [Fact]
    public async Task Dung_lai_refresh_token_da_xoay_vong_thu_hoi_moi_phien()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);
        var moi = await HttpTestExtensions.DocPhienAsync(await _client.GuiAsync(
            HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie, csrf: true));

        // Giả lập token cũ bị đánh cắp và dùng lại sau khoảng tranh chấp giữa các tab.
        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand(
                "UPDATE dbo.PHIENDANGNHAP SET THUHOILUC = DATEADD(MINUTE, -5, THUHOILUC), NGAYTAO = DATEADD(MINUTE, -5, NGAYTAO) WHERE MAPHIENTHAY IS NOT NULL;",
                connection);
            await command.ExecuteNonQueryAsync();
        }

        var keTanCong = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, keTanCong.StatusCode);

        var nguoiDung = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: moi.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, nguoiDung.StatusCode);
    }

    [Fact]
    public async Task Dang_xuat_thu_hoi_phien_va_xoa_cookie()
    {
        var phien = await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        var dangXuat = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/dang-xuat", refreshCookie: phien.RefreshCookie, csrf: true);

        Assert.Equal(HttpStatusCode.NoContent, dangXuat.StatusCode);
        var xoa = dangXuat.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("ce_refresh=", StringComparison.Ordinal));
        Assert.Contains("expires=Thu, 01 Jan 1970", xoa);

        var lamMoi = await _client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/lam-moi", refreshCookie: phien.RefreshCookie, csrf: true);
        Assert.Equal(HttpStatusCode.Unauthorized, lamMoi.StatusCode);
    }

    [Fact]
    public async Task Ghi_nhat_ky_dang_nhap_kem_dia_chi_ip()
    {
        await _client.DangNhapAsync("admin", "SaiMatKhau1");
        await _client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin);

        await using var db = database.CreateDbContext();
        var nhatKy = await db.NhatKyHoatDongs.Where(x => x.Nhom == "XACTHUC").OrderBy(x => x.MaNhatKy).ToListAsync();
        Assert.Equal(["Đăng nhập thất bại", "Đăng nhập"], nhatKy.Select(x => x.HanhDong));
        Assert.All(nhatKy, x => Assert.NotNull(x.MaTK));
    }

    [Fact]
    public async Task Gioi_han_so_lan_dang_nhap_theo_ip()
    {
        await using var factory = new CampusExamApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?> { ["Auth:LoginPermitsPerMinute"] = "2" });
        using var client = factory.TaoClient();

        await client.DangNhapAsync("admin", "SaiMatKhau1");
        await client.DangNhapAsync("admin", "SaiMatKhau1");
        var lanBa = await client.PostAsJsonAsync("/api/xac-thuc/dang-nhap", new { tenDN = "admin", matKhau = "x" });

        Assert.Equal(HttpStatusCode.TooManyRequests, lanBa.StatusCode);
        Assert.Equal("RATE_LIMITED", await lanBa.MaLoiAsync());
        Assert.True(lanBa.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Sau_reverse_proxy_tin_cay_rate_limit_tinh_theo_ip_that_cua_nguoi_dung()
    {
        await using var factory = new CampusExamApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["Auth:LoginPermitsPerMinute"] = "2",
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.1"
            });
        using var client = factory.TaoClient();

        Task<HttpResponseMessage> DangNhapQuaProxy(string ipNguoiDung) =>
            GuiQuaProxyAsync(client, proxy: "10.0.0.1", xForwardedFor: ipNguoiDung);

        await DangNhapQuaProxy("203.0.113.5");
        await DangNhapQuaProxy("203.0.113.5");

        // Máy khác sau cùng proxy có hạn mức riêng.
        Assert.Equal(HttpStatusCode.Unauthorized, (await DangNhapQuaProxy("203.0.113.6")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await DangNhapQuaProxy("203.0.113.5")).StatusCode);
    }

    [Fact]
    public async Task Bo_qua_x_forwarded_for_tu_nguon_khong_tin_cay()
    {
        await using var factory = new CampusExamApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["Auth:LoginPermitsPerMinute"] = "2",
                ["ReverseProxy:KnownProxies:0"] = "10.0.0.1"
            });
        using var client = factory.TaoClient();

        // Kẻ tấn công tự đặt X-Forwarded-For khác nhau để né hạn mức: không có tác dụng.
        await GuiQuaProxyAsync(client, proxy: "198.51.100.9", xForwardedFor: "1.1.1.1");
        await GuiQuaProxyAsync(client, proxy: "198.51.100.9", xForwardedFor: "2.2.2.2");
        var lanBa = await GuiQuaProxyAsync(client, proxy: "198.51.100.9", xForwardedFor: "3.3.3.3");

        Assert.Equal(HttpStatusCode.TooManyRequests, lanBa.StatusCode);
    }

    [Fact]
    public async Task Han_muc_dang_nhap_mac_dinh_du_cho_phong_may_dung_chung_ip()
    {
        await using var factory = new CampusExamApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?> { ["Auth:LoginPermitsPerMinute"] = null });
        using var client = factory.TaoClient();

        for (var lan = 1; lan <= 30; lan++)
        {
            var phien = await client.DangNhapAsync($"thisinh{lan}", "SaiMatKhau1", ip: "10.1.1.1");
            Assert.Equal(HttpStatusCode.Unauthorized, phien.Response.StatusCode);
        }
    }

    [Fact]
    public async Task Kiem_tra_token_dung_cache_ngan_han_va_tat_duoc_bang_cau_hinh()
    {
        async Task<(HttpStatusCode TruocKhi, HttpStatusCode SauKhi)> DoiMaBaoMatNgoaiApi(string? giayCache)
        {
            await database.ResetAsync();
            await DuLieuXacThuc.TaoAdminAsync(database);
            await using var factory = new CampusExamApiFactory(
                database.ConnectionString,
                new Dictionary<string, string?> { ["Auth:TokenCheckCacheSeconds"] = giayCache });
            using var client = factory.TaoClient();
            var token = await client.LayTokenAdminAsync();
            var truocKhi = (await client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", token)).StatusCode;

            // Giả lập thay đổi từ nơi khác (instance khác, thao tác trực tiếp database).
            await using (var connection = new SqlConnection(database.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand("UPDATE dbo.TAIKHOAN SET MABAOMAT = NEWID();", connection);
                await command.ExecuteNonQueryAsync();
            }

            return (truocKhi, (await client.GuiAsync(HttpMethod.Get, "/api/xac-thuc/toi", token)).StatusCode);
        }

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), await DoiMaBaoMatNgoaiApi(null));
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.Unauthorized), await DoiMaBaoMatNgoaiApi("0"));
    }

    private static Task<HttpResponseMessage> GuiQuaProxyAsync(HttpClient client, string proxy, string xForwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/xac-thuc/dang-nhap")
        {
            Content = JsonContent.Create(new { tenDN = "admin", matKhau = "SaiMatKhau1" })
        };
        request.Headers.Add(CampusExamApiFactory.HeaderIP, proxy);
        request.Headers.Add("X-Forwarded-For", xForwardedFor);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task Doi_mat_khau_cung_bi_gioi_han_so_lan()
    {
        await using var factory = new CampusExamApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?> { ["Auth:LoginPermitsPerMinute"] = "2" });
        using var client = factory.TaoClient();
        var token = (await client.DangNhapAsync("admin", DuLieuXacThuc.MatKhauAdmin)).AccessToken;

        await client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", token,
            body: new { matKhauCu = "DoMatKhau1", matKhauMoi = "MatKhauMoi1" });
        var lanBa = await client.GuiAsync(HttpMethod.Post, "/api/xac-thuc/doi-mat-khau", token,
            body: new { matKhauCu = "DoMatKhau2", matKhauMoi = "MatKhauMoi1" });

        Assert.Equal(HttpStatusCode.TooManyRequests, lanBa.StatusCode);
    }
}
