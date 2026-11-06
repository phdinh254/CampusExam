using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class NhatKyHoatDongConfiguration : IEntityTypeConfiguration<NhatKyHoatDong>
{
    public void Configure(EntityTypeBuilder<NhatKyHoatDong> entity)
    {
        entity.ToTable("NHATKYHD", "dbo");

        entity.HasKey(x => x.MaNhatKy).HasName("PK_NHATKYHD");
        entity.HasIndex(x => x.MaTK).HasDatabaseName("GHI_NHAT_KY_FK");
        entity.HasIndex(x => x.ThoiGian).HasDatabaseName("IX_NHATKYHD_THOIGIAN");

        entity.Property(x => x.MaNhatKy).HasColumnName("MANHATKY").UseIdentityColumn();
        entity.Property(x => x.MaTK).HasColumnName("MTK");
        entity.Property(x => x.Nhom).HasColumnName("NHOM").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false);
        entity.Property(x => x.HanhDong).HasColumnName("HANHDONG").HasColumnType("nvarchar(200)").HasMaxLength(200);
        entity.Property(x => x.DoiTuong).HasColumnName("DOITUONG").HasColumnType("nvarchar(200)").HasMaxLength(200);
        entity.Property(x => x.ChiTiet).HasColumnName("CHITIET").HasColumnType("nvarchar(2000)").HasMaxLength(2000);
        entity.Property(x => x.ThoiGian).HasColumnName("THOIGIAN");
        entity.Property(x => x.DiaChiIP).HasColumnName("DIACHIIP").HasColumnType("varchar(45)").HasMaxLength(45).IsUnicode(false);

        entity.HasOne<TaiKhoan>()
            .WithMany()
            .HasForeignKey(x => x.MaTK)
            .HasConstraintName("FK_NHATKYHD_GHI_NHAT__TAIKHOAN")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
