using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class PhienDangNhapConfiguration : IEntityTypeConfiguration<PhienDangNhap>
{
    public void Configure(EntityTypeBuilder<PhienDangNhap> entity)
    {
        entity.ToTable("PHIENDANGNHAP", "dbo");

        entity.HasKey(x => x.MaPhien).HasName("PK_PHIENDANGNHAP");
        entity.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("AK_PHIENDANGNHAP_TOKENHASH");
        entity.HasIndex(x => x.MaTK).HasDatabaseName("IX_PHIENDANGNHAP_MTK");

        entity.Property(x => x.MaPhien).HasColumnName("MAPHIEN").UseIdentityColumn();
        entity.Property(x => x.MaTK).HasColumnName("MTK");
        entity.Property(x => x.TokenHash).HasColumnName("TOKENHASH").HasColumnType("binary(32)").HasMaxLength(32).IsFixedLength();
        entity.Property(x => x.NgayTao).HasColumnName("NGAYTAO");
        entity.Property(x => x.HetHan).HasColumnName("HETHAN");
        entity.Property(x => x.ThuHoiLuc).HasColumnName("THUHOILUC");
        entity.Property(x => x.MaPhienThay).HasColumnName("MAPHIENTHAY");
        entity.Property(x => x.DiaChiIP).HasColumnName("DIACHIIP").HasColumnType("varchar(45)").HasMaxLength(45).IsUnicode(false);
        entity.Property(x => x.TrinhDuyet).HasColumnName("TRINHDUYET").HasColumnType("nvarchar(300)").HasMaxLength(300);

        entity.HasOne<TaiKhoan>()
            .WithMany()
            .HasForeignKey(x => x.MaTK)
            .HasConstraintName("FK_PHIENDANGNHAP_TAIKHOAN")
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne<PhienDangNhap>()
            .WithMany()
            .HasForeignKey(x => x.MaPhienThay)
            .HasConstraintName("FK_PHIENDANGNHAP_THAYTHE")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
