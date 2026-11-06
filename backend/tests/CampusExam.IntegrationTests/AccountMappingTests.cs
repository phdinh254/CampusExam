using CampusExam.Domain.Entities;
using CampusExam.IntegrationTests.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusExam.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class AccountMappingTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TaiKhoan_round_trips_all_columns_and_returns_utc_times()
    {
        var now = TruncateToMicroseconds(DateTime.UtcNow);
        var maBaoMat = Guid.NewGuid();
        int maTK;

        await using (var write = database.CreateDbContext())
        {
            var taiKhoan = new TaiKhoan
            {
                TenDN = "admin",
                MatKhauHash = "hash",
                VaiTro = TaiKhoan.VaiTroAdmin,
                HoTen = "Quản trị viên",
                TrangThai = TaiKhoan.TrangThaiHoatDong,
                LanDNCuoi = now,
                NgayTao = now,
                PhaiDoiMatKhau = true,
                SoLanDNSai = 2,
                KhoaDen = now.AddMinutes(15),
                MaBaoMat = maBaoMat
            };
            write.TaiKhoans.Add(taiKhoan);
            await write.SaveChangesAsync();
            maTK = taiKhoan.MaTK;
        }

        await using var read = database.CreateDbContext();
        var saved = await read.TaiKhoans.SingleAsync(x => x.MaTK == maTK);

        Assert.Equal("admin", saved.TenDN);
        Assert.Equal("hash", saved.MatKhauHash);
        Assert.Equal("ADMIN", saved.VaiTro);
        Assert.Equal("Quản trị viên", saved.HoTen);
        Assert.Equal(TaiKhoan.TrangThaiHoatDong, saved.TrangThai);
        Assert.True(saved.PhaiDoiMatKhau);
        Assert.Equal(2, saved.SoLanDNSai);
        Assert.Equal(maBaoMat, saved.MaBaoMat);
        Assert.Equal(now, saved.NgayTao);
        Assert.Equal(DateTimeKind.Utc, saved.NgayTao.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.LanDNCuoi!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.KhoaDen!.Value.Kind);
    }

    [Fact]
    public async Task PhienDangNhap_stores_token_hash_and_rotation_link()
    {
        var now = TruncateToMicroseconds(DateTime.UtcNow);
        await using var dbContext = database.CreateDbContext();
        var taiKhoan = NewAccount("sv001", now);
        dbContext.TaiKhoans.Add(taiKhoan);
        await dbContext.SaveChangesAsync();

        var cu = NewSession(taiKhoan.MaTK, 1, now);
        dbContext.PhienDangNhaps.Add(cu);
        await dbContext.SaveChangesAsync();

        var moi = NewSession(taiKhoan.MaTK, 2, now);
        dbContext.PhienDangNhaps.Add(moi);
        await dbContext.SaveChangesAsync();

        cu.ThuHoiLuc = now.AddMinutes(1);
        cu.MaPhienThay = moi.MaPhien;
        await dbContext.SaveChangesAsync();

        await using var read = database.CreateDbContext();
        var saved = await read.PhienDangNhaps.SingleAsync(x => x.MaPhien == cu.MaPhien);
        Assert.Equal(cu.TokenHash, saved.TokenHash);
        Assert.Equal(moi.MaPhien, saved.MaPhienThay);
        Assert.Equal(DateTimeKind.Utc, saved.ThuHoiLuc!.Value.Kind);
        Assert.Equal("127.0.0.1", saved.DiaChiIP);
    }

    [Fact]
    public async Task NhatKyHoatDong_allows_system_entries_without_account()
    {
        var now = TruncateToMicroseconds(DateTime.UtcNow);
        await using (var write = database.CreateDbContext())
        {
            write.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                MaTK = null,
                Nhom = "HETHONG",
                HanhDong = "Khởi tạo",
                DoiTuong = "Hệ thống",
                ChiTiet = "Chi tiết",
                ThoiGian = now
            });
            await write.SaveChangesAsync();
        }

        await using var read = database.CreateDbContext();
        var saved = await read.NhatKyHoatDongs.SingleAsync();
        Assert.Null(saved.MaTK);
        Assert.Equal("Khởi tạo", saved.HanhDong);
        Assert.Equal(now, saved.ThoiGian);
    }

    private static TaiKhoan NewAccount(string tenDN, DateTime now) => new()
    {
        TenDN = tenDN,
        MatKhauHash = "hash",
        VaiTro = TaiKhoan.VaiTroThiSinh,
        HoTen = "Thí sinh",
        TrangThai = TaiKhoan.TrangThaiHoatDong,
        NgayTao = now,
        MaBaoMat = Guid.NewGuid()
    };

    private static PhienDangNhap NewSession(int maTK, byte seed, DateTime now) => new()
    {
        MaTK = maTK,
        TokenHash = Enumerable.Repeat(seed, 32).ToArray(),
        NgayTao = now,
        HetHan = now.AddDays(7),
        DiaChiIP = "127.0.0.1",
        TrinhDuyet = "test"
    };

    // datetime2 mặc định lưu 7 chữ số; giữ nguyên để so sánh chính xác.
    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
}
