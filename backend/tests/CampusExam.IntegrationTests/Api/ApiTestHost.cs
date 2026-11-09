using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusExam.Application.Accounts;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using CampusExam.Infrastructure.Security;
using CampusExam.IntegrationTests.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusExam.IntegrationTests.Api;

/// <summary>API chạy trong bộ nhớ, trỏ vào database tạm của <see cref="SqlServerDatabaseFixture"/>.</summary>
public sealed class CampusExamApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? extraSettings = null) : WebApplicationFactory<Program>
{
    public const string SigningKey = "khoa-ky-chi-dung-cho-integration-test-0123456789";
    public const string AllowedOrigin = "http://localhost:5173";

    private static readonly ConcurrentDictionary<string, CampusExamApiFactory> Shared = new();

    /// <summary>Thư mục storage riêng của host này (ảnh câu hỏi, tệp nhập tạm); xóa khi dispose.</summary>
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "CampusExam_Test_Storage", Guid.NewGuid().ToString("N"));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }

    /// <summary>Dùng chung một host cho mọi test trên cùng database để test chạy nhanh.</summary>
    public static CampusExamApiFactory For(SqlServerDatabaseFixture database) =>
        Shared.GetOrAdd(database.ConnectionString, connection => new CampusExamApiFactory(connection));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:CampusExam"] = connectionString,
            ["Storage:Root"] = StorageRoot,
            ["Auth:SigningKey"] = SigningKey,
            ["Auth:LoginPermitsPerMinute"] = "10000",
            ["Auth:AllowedOrigins:0"] = AllowedOrigin
        };
        // Giá trị null: bỏ cấu hình mặc định của test để API dùng giá trị trong appsettings/AuthOptions.
        foreach (var (key, value) in extraSettings ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                settings.Remove(key);
            }
            else
            {
                settings[key] = value;
            }
        }

        builder.UseEnvironment("Development");
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        // Thêm cuối cùng để đè User Secrets của máy phát triển.
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, GiaLapDiaChiIP>());
    }

    public const string HeaderIP = "X-Test-IP";

    /// <summary>
    /// TestServer không có IP kết nối; header <see cref="HeaderIP"/> đặt IP cho từng request
    /// để test khóa đăng nhập theo cặp tài khoản + IP. Chạy trước mọi middleware của API.
    /// </summary>
    private sealed class GiaLapDiaChiIP : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers[HeaderIP], out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }

    /// <summary>Client HTTPS (cookie Secure), tự quản lý cookie để test kiểm soát rõ ràng.</summary>
    public HttpClient TaoClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
            AllowAutoRedirect = false
        });

        // Chặn trường hợp test vô tình chạy trên database phát triển.
        using var scope = Services.CreateScope();
        var actual = new SqlConnectionStringBuilder(
            scope.ServiceProvider.GetRequiredService<CampusExamDbContext>().Database.GetConnectionString()).InitialCatalog;
        var expected = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (actual != expected)
        {
            throw new InvalidOperationException($"API test đang dùng sai database: {actual}, cần {expected}.");
        }

        return client;
    }
}

public static class DuLieuXacThuc
{
    public const string TenAdmin = "admin";
    public const string MatKhauAdmin = "QuanTri2026x";
    public const string Msts = "SV001";
    public static readonly DateOnly NgaySinh = new(2005, 3, 14);
    public static readonly string MatKhauMacDinh = ChinhSachMatKhau.MatKhauMacDinh(NgaySinh);

    private static readonly AspNetPasswordHasher Hasher = new();

    public static async Task<int> TaoAdminAsync(
        SqlServerDatabaseFixture database,
        string tenDN = TenAdmin,
        string matKhau = MatKhauAdmin,
        short trangThai = TaiKhoan.TrangThaiHoatDong)
    {
        await using var db = database.CreateDbContext();
        var taiKhoan = TaoTaiKhoan(tenDN, matKhau, TaiKhoan.VaiTroAdmin, "Nguyễn Thảo", trangThai, phaiDoi: false);
        db.TaiKhoans.Add(taiKhoan);
        await db.SaveChangesAsync();
        return taiKhoan.MaTK;
    }

    /// <summary>Thí sinh mới theo D-THISINH-01: tên đăng nhập = MSTS, mật khẩu = ngày sinh, bắt buộc đổi.</summary>
    public static async Task<int> TaoThiSinhAsync(SqlServerDatabaseFixture database, string msts = Msts)
    {
        await using var db = database.CreateDbContext();
        var taiKhoan = TaoTaiKhoan(msts, ChinhSachMatKhau.MatKhauMacDinh(NgaySinh), TaiKhoan.VaiTroThiSinh,
            "Nguyễn Minh Long", TaiKhoan.TrangThaiHoatDong, phaiDoi: true);
        db.TaiKhoans.Add(taiKhoan);
        await db.SaveChangesAsync();

        db.ThiSinhs.Add(new ThiSinh
        {
            Msts = msts,
            MaTK = taiKhoan.MaTK,
            HoTen = "Nguyễn Minh Long",
            NgaySinh = NgaySinh,
            GioiTinh = ThiSinh.GioiTinhNam,
            Lop = "K29-CNPM",
            NgayTao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return taiKhoan.MaTK;
    }

    private static TaiKhoan TaoTaiKhoan(string tenDN, string matKhau, string vaiTro, string hoTen, short trangThai, bool phaiDoi) =>
        new()
        {
            TenDN = tenDN,
            MatKhauHash = Hasher.Hash(matKhau),
            VaiTro = vaiTro,
            HoTen = hoTen,
            TrangThai = trangThai,
            NgayTao = DateTime.UtcNow,
            PhaiDoiMatKhau = phaiDoi,
            MaBaoMat = Guid.NewGuid()
        };
}

/// <summary>Kết quả đăng nhập đã tách sẵn token và cookie.</summary>
public sealed record PhienTest(HttpResponseMessage Response, JsonDocument Body, string? AccessToken, string? RefreshCookie)
{
    public string? SetCookieHeader => Response.Headers.TryGetValues("Set-Cookie", out var values)
        ? values.FirstOrDefault(value => value.StartsWith("ce_refresh=", StringComparison.Ordinal))
        : null;
}

public static class HttpTestExtensions
{
    public static async Task<PhienTest> DangNhapAsync(
        this HttpClient client,
        string tenDN,
        string matKhau,
        bool ghiNho = false,
        string? ip = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/xac-thuc/dang-nhap")
        {
            Content = JsonContent.Create(new { tenDN, matKhau, ghiNho })
        };
        if (ip is not null)
        {
            request.Headers.Add(CampusExamApiFactory.HeaderIP, ip);
        }

        return await DocPhienAsync(await client.SendAsync(request));
    }

    public static async Task<string> LayTokenAdminAsync(this HttpClient client)
    {
        var phien = await client.DangNhapAsync(DuLieuXacThuc.TenAdmin, DuLieuXacThuc.MatKhauAdmin);
        return phien.AccessToken ?? throw new InvalidOperationException("Đăng nhập admin thất bại trong test.");
    }

    public static Task<HttpResponseMessage> GuiAsync(
        this HttpClient client,
        HttpMethod method,
        string url,
        string? accessToken = null,
        string? refreshCookie = null,
        object? body = null,
        bool csrf = false,
        string? origin = null,
        string? ip = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (ip is not null)
        {
            request.Headers.Add(CampusExamApiFactory.HeaderIP, ip);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (refreshCookie is not null)
        {
            request.Headers.Add("Cookie", $"ce_refresh={refreshCookie}");
        }

        if (csrf)
        {
            request.Headers.Add("X-CSRF", "1");
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    public static async Task<PhienTest> DocPhienAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(string.IsNullOrEmpty(text) ? "{}" : text);
        var accessToken = body.RootElement.TryGetProperty("accessToken", out var token) ? token.GetString() : null;

        string? cookie = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var header = values.FirstOrDefault(value => value.StartsWith("ce_refresh=", StringComparison.Ordinal));
            var value = header?["ce_refresh=".Length..].Split(';')[0];
            cookie = string.IsNullOrEmpty(value) ? null : value;
        }

        return new PhienTest(response, body, accessToken, cookie);
    }

    public static async Task<string?> MaLoiAsync(this HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
