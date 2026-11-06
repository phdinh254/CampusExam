using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Queries;
using CampusExam.IntegrationTests.Database;
using Xunit;

namespace CampusExam.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class EfQueryTests(SqlServerDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MucDo_returns_seeded_levels_in_order()
    {
        await using var dbContext = database.CreateDbContext();

        var result = await new EfMucDoQuery(dbContext).GetAllAsync(CancellationToken.None);

        Assert.Equal(
            [(1, "Dễ"), (2, "Trung bình"), (3, "Khó")],
            result.Select(item => ((int)item.MaMD, item.TenMD)));
    }

    [Fact]
    public async Task Kqht_list_is_ordered_by_thu_tu_and_detail_reads_single_item()
    {
        await using (var seed = database.CreateDbContext())
        {
            seed.KqHocTaps.AddRange(
                new KqHocTap { TenKQHT = "Vận dụng", ThuTu = 3, TrangThai = 1, KyHieu = "C" },
                new KqHocTap { TenKQHT = "Nhớ", ThuTu = 1, MoTa = "Ghi nhớ", TrangThai = 1, KyHieu = "A" },
                new KqHocTap { TenKQHT = "Hiểu", ThuTu = 2, TrangThai = 0, KyHieu = "B" });
            await seed.SaveChangesAsync();
        }

        await using var dbContext = database.CreateDbContext();
        var query = new EfKqhtQuery(dbContext);

        var list = await query.GetAllAsync(CancellationToken.None);
        Assert.Equal(["A", "B", "C"], list.Select(item => item.KyHieu));

        var first = list[0];
        var detail = await query.GetByIdAsync(first.MaKQHT, CancellationToken.None);
        Assert.Equal(first, detail);
        Assert.Equal("Ghi nhớ", detail!.MoTa);

        Assert.Null(await query.GetByIdAsync(int.MaxValue, CancellationToken.None));
    }
}
