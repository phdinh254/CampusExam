using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class TaiNguyenCauHoiMappingTests
{
    [Fact]
    public void Model_matches_TAINGUYEN_CAUHOI_table_definition()
    {
        var options = new DbContextOptionsBuilder<CampusExamDbContext>()
            .UseSqlServer("Server=(local);Database=CampusExam;Trusted_Connection=True")
            .Options;

        using var dbContext = new CampusExamDbContext(options);
        var entity = dbContext.Model.FindEntityType(typeof(TaiNguyenCauHoi));

        Assert.NotNull(entity);
        Assert.Equal("TAINGUYEN_CAUHOI", entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());
        Assert.Equal(
            nameof(TaiNguyenCauHoi.MaTN),
            entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(ValueGenerated.OnAdd, entity.FindProperty(
            nameof(TaiNguyenCauHoi.MaTN))!.ValueGenerated);

        var alternateKeys = entity.GetKeys()
            .Where(key => !key.IsPrimaryKey())
            .Select(key => key.GetName())
            .ToHashSet();
        Assert.Contains("AK_TAINGUYEN_CAUHOI_STORAGEKEY", alternateKeys);
        Assert.Contains("AK_TAINGUYEN_CAUHOI_VITRI", alternateKeys);

        Assert.Equal(
            "varchar(500)",
            entity.FindProperty(nameof(TaiNguyenCauHoi.StorageKey))!.GetColumnType());
        Assert.Equal(
            "nvarchar(500)",
            entity.FindProperty(nameof(TaiNguyenCauHoi.AltText))!.GetColumnType());
    }
}
