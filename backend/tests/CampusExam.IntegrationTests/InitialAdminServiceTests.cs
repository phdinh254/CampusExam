using CampusExam.Application.Accounts;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Commands;
using CampusExam.Infrastructure.Security;
using CampusExam.IntegrationTests.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusExam.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class InitialAdminServiceTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    private readonly AspNetPasswordHasher _hasher = new();

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Creates_active_admin_with_hashed_password_and_audit_entry()
    {
        await using var dbContext = database.CreateDbContext();
        var service = new EfInitialAdminService(dbContext, _hasher);

        var result = await service.CreateAsync(Command(" Admin "), CancellationToken.None);

        Assert.Equal(CreateInitialAdminStatus.Created, result.Status);

        await using var read = database.CreateDbContext();
        var admin = await read.TaiKhoans.SingleAsync();
        Assert.Equal(result.MaTK, admin.MaTK);
        Assert.Equal("admin", admin.TenDN);
        Assert.Equal(TaiKhoan.VaiTroAdmin, admin.VaiTro);
        Assert.Equal(TaiKhoan.TrangThaiHoatDong, admin.TrangThai);
        Assert.False(admin.PhaiDoiMatKhau);
        Assert.NotEqual(Guid.Empty, admin.MaBaoMat);
        Assert.DoesNotContain("MatKhau2026", admin.MatKhauHash);
        Assert.Equal(PasswordCheck.Success, _hasher.Verify(admin.MatKhauHash, "MatKhau2026"));
        Assert.Equal(PasswordCheck.Failed, _hasher.Verify(admin.MatKhauHash, "SaiMatKhau2026"));

        var log = await read.NhatKyHoatDongs.SingleAsync();
        Assert.Equal(admin.MaTK, log.MaTK);
        Assert.Equal("TAIKHOAN", log.Nhom);
        Assert.DoesNotContain("MatKhau2026", log.ChiTiet ?? string.Empty);
    }

    [Fact]
    public async Task Refuses_when_an_admin_already_exists()
    {
        await using var dbContext = database.CreateDbContext();
        var service = new EfInitialAdminService(dbContext, _hasher);
        await service.CreateAsync(Command("admin"), CancellationToken.None);

        var result = await service.CreateAsync(Command("admin2"), CancellationToken.None);

        Assert.Equal(CreateInitialAdminStatus.AdminAlreadyExists, result.Status);
        Assert.Null(result.MaTK);
        Assert.Equal(1, await dbContext.TaiKhoans.CountAsync());
    }

    [Fact]
    public async Task Refuses_ten_dn_used_by_candidate_account()
    {
        await using (var seed = database.CreateDbContext())
        {
            seed.TaiKhoans.Add(new TaiKhoan
            {
                TenDN = "admin",
                MatKhauHash = "x",
                VaiTro = TaiKhoan.VaiTroThiSinh,
                HoTen = "Thí sinh",
                TrangThai = TaiKhoan.TrangThaiHoatDong,
                NgayTao = DateTime.UtcNow,
                MaBaoMat = Guid.NewGuid()
            });
            await seed.SaveChangesAsync();
        }

        await using var dbContext = database.CreateDbContext();
        var result = await new EfInitialAdminService(dbContext, _hasher)
            .CreateAsync(Command("ADMIN"), CancellationToken.None);

        Assert.Equal(CreateInitialAdminStatus.DuplicateTenDN, result.Status);
        Assert.Equal(1, await dbContext.TaiKhoans.CountAsync());
    }

    private static CreateInitialAdminCommand Command(string tenDN) =>
        new() { TenDN = tenDN, HoTen = "Quản trị viên", MatKhau = "MatKhau2026" };
}
