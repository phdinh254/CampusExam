using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class LanImportConfiguration : IEntityTypeConfiguration<LanImport>
{
    public void Configure(EntityTypeBuilder<LanImport> entity)
    {
        entity.ToTable("LANIMPORT", "dbo");

        entity.HasKey(x => x.MaLanImport).HasName("PK_LANIMPORT");
        entity.HasIndex(x => x.MaTK).HasDatabaseName("THUC_HIEN_FK");

        entity.Property(x => x.MaLanImport).HasColumnName("MALANIMPORT").UseIdentityColumn();
        entity.Property(x => x.MaTK).HasColumnName("MTK");
        entity.Property(x => x.LoaiImport).HasColumnName("LOAIIMPORT").HasColumnType("nvarchar(10)").HasMaxLength(10);
        entity.Property(x => x.TenFile).HasColumnName("TENFILE").HasColumnType("nvarchar(255)").HasMaxLength(255);
        entity.Property(x => x.TongDong).HasColumnName("TONGDONG");
        entity.Property(x => x.SoThanhCong).HasColumnName("SOTHANHCONG");
        entity.Property(x => x.SoDongLoi).HasColumnName("SODONGLOI");
        entity.Property(x => x.ThoiGian).HasColumnName("THOIGIAN");
        entity.Property(x => x.TrangThai).HasColumnName("TRANGTHAILANIMPORT").HasColumnType("tinyint");
    }
}
