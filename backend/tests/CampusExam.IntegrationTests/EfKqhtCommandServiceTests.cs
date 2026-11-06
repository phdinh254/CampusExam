using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Commands;
using CampusExam.IntegrationTests.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusExam.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class EfKqhtCommandServiceTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_normalizes_values_and_returns_created_item()
    {
        await using var dbContext = database.CreateDbContext();
        var service = new EfKqhtCommandService(dbContext);
        var command = new CreateKqhtCommand
        {
            TenKQHT = "  Nhớ  ",
            ThuTu = 1,
            MoTa = "  Mô tả  ",
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = "a"
        };

        var result = await service.CreateAsync(command, CancellationToken.None);

        Assert.Equal(CreateKqhtStatus.Created, result.Status);
        Assert.NotNull(result.Kqht);
        Assert.True(result.Kqht.MaKQHT > 0);
        Assert.Equal("Nhớ", result.Kqht.TenKQHT);
        Assert.Equal("Mô tả", result.Kqht.MoTa);
        Assert.Equal("A", result.Kqht.KyHieu);

        await using var verify = database.CreateDbContext();
        var saved = await verify.KqHocTaps.SingleAsync();
        Assert.Equal(result.Kqht.MaKQHT, saved.MaKQHT);
        Assert.Equal("Nhớ", saved.TenKQHT);
        Assert.Equal("A", saved.KyHieu);
    }

    [Fact]
    public async Task Create_returns_duplicate_when_normalized_ky_hieu_exists()
    {
        await using var dbContext = database.CreateDbContext();
        var service = new EfKqhtCommandService(dbContext);
        var first = new CreateKqhtCommand
        {
            TenKQHT = "Nhớ",
            ThuTu = 1,
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = "A"
        };
        var duplicate = new CreateKqhtCommand
        {
            TenKQHT = "Hiểu",
            ThuTu = 1,
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = "a"
        };

        await service.CreateAsync(first, CancellationToken.None);
        var result = await service.CreateAsync(duplicate, CancellationToken.None);

        Assert.Equal(CreateKqhtStatus.DuplicateKyHieu, result.Status);
        Assert.Null(result.Kqht);
        Assert.Equal(1, await dbContext.KqHocTaps.CountAsync());
    }

    [Fact]
    public async Task Database_rejects_ky_hieu_not_matching_thu_tu()
    {
        // Bỏ qua validation của command để chứng minh database tự chặn.
        await using var dbContext = database.CreateDbContext();
        dbContext.KqHocTaps.Add(new KqHocTap
        {
            TenKQHT = "Sai ký hiệu",
            ThuTu = 2,
            TrangThai = 1,
            KyHieu = "A"
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CKT_KQHOCTAP_KYHIEU", sqlException.Message);
    }
}
