using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class KqHocTapMappingTests
{
    [Fact]
    public void Model_matches_KQHOCTAP_table_definition()
    {
        var options = new DbContextOptionsBuilder<CampusExamDbContext>()
            .UseSqlServer("Server=(local);Database=CampusExam;Trusted_Connection=True")
            .Options;

        using var dbContext = new CampusExamDbContext(options);
        var entity = dbContext.Model.FindEntityType(typeof(KqHocTap));

        Assert.NotNull(entity);
        Assert.Equal("KQHOCTAP", entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());

        AssertProperty(entity, nameof(KqHocTap.MaKQHT), "MAKQHT", "int", false);
        AssertProperty(entity, nameof(KqHocTap.TenKQHT), "TENKQHT", "nvarchar(100)", false);
        AssertProperty(entity, nameof(KqHocTap.ThuTu), "THUTU", "tinyint", false);
        AssertProperty(entity, nameof(KqHocTap.MoTa), "MOTA", "nvarchar(1000)", true);
        AssertProperty(entity, nameof(KqHocTap.TrangThai), "TRANGTHAI", "smallint", false);
        AssertProperty(entity, nameof(KqHocTap.KyHieu), "KYHIEU", "char(1)", false);

        Assert.Equal(
            ValueGenerated.OnAdd,
            entity.FindProperty(nameof(KqHocTap.MaKQHT))!.ValueGenerated);
        Assert.Equal(
            nameof(KqHocTap.MaKQHT),
            entity.FindPrimaryKey()!.Properties.Single().Name);

        var kyHieuKey = entity.GetKeys().Single(key => !key.IsPrimaryKey());
        Assert.Equal(nameof(KqHocTap.KyHieu), kyHieuKey.Properties.Single().Name);
    }

    private static void AssertProperty(
        IReadOnlyEntityType entity,
        string propertyName,
        string columnName,
        string columnType,
        bool isNullable)
    {
        var property = entity.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName());
        Assert.Equal(columnType, property.GetColumnType());
        Assert.Equal(isNullable, property.IsNullable);
    }
}
