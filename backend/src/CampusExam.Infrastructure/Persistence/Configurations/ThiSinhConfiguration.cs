using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class ThiSinhConfiguration : IEntityTypeConfiguration<ThiSinh>
{
    public void Configure(EntityTypeBuilder<ThiSinh> entity)
    {
        entity.ToTable("THISINH", "dbo");

        entity.HasKey(x => x.Msts).HasName("PK_THISINH");
        entity.HasIndex(x => x.MaTK).IsUnique().HasDatabaseName("AK_AK_THISINH_MTK_THISINH");
        entity.HasIndex(x => x.MaLanImport).HasDatabaseName("NHAP_THI_SINH_FK");

        entity.Property(x => x.Msts).HasColumnName("MSTS").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false);
        entity.Property(x => x.MaTK).HasColumnName("MTK");
        entity.Property(x => x.MaLanImport).HasColumnName("MALANIMPORT");
        entity.Property(x => x.HoTen).HasColumnName("HOTEN").HasColumnType("nvarchar(100)").HasMaxLength(100);
        entity.Property(x => x.NgaySinh).HasColumnName("NGAYSINH").HasColumnType("date");
        entity.Property(x => x.GioiTinh).HasColumnName("GIOITINH").HasColumnType("smallint");
        entity.Property(x => x.Lop).HasColumnName("LOP").HasColumnType("nvarchar(50)").HasMaxLength(50);
        entity.Property(x => x.NoiSinh).HasColumnName("NOISINH").HasColumnType("nvarchar(200)").HasMaxLength(200);
        entity.Property(x => x.Cccd).HasColumnName("CCCD").HasColumnType("char(12)").HasMaxLength(12).IsFixedLength().IsUnicode(false);
        entity.Property(x => x.NgayTao).HasColumnName("NGAYTAO");

        entity.HasOne<TaiKhoan>()
            .WithOne()
            .HasForeignKey<ThiSinh>(x => x.MaTK)
            .HasConstraintName("FK_THISINH_SO_HUU2_TAIKHOAN")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
