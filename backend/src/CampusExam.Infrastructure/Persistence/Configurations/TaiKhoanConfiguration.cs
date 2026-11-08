using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class TaiKhoanConfiguration : IEntityTypeConfiguration<TaiKhoan>
{
    public void Configure(EntityTypeBuilder<TaiKhoan> entity)
    {
        entity.ToTable("TAIKHOAN", "dbo");

        entity.HasKey(x => x.MaTK).HasName("PK_TAIKHOAN");
        entity.HasIndex(x => x.TenDN).IsUnique().HasDatabaseName("AK_AK_TAIKHOAN_TENDN_TAIKHOAN");

        entity.Property(x => x.MaTK).HasColumnName("MTK").UseIdentityColumn();
        entity.Property(x => x.TenDN).HasColumnName("TENDN").HasColumnType("varchar(50)").HasMaxLength(50).IsUnicode(false);
        entity.Property(x => x.MatKhauHash).HasColumnName("MATKHAUHASH").HasColumnType("varchar(255)").HasMaxLength(255).IsUnicode(false);
        entity.Property(x => x.VaiTro).HasColumnName("VAITRO").HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false);
        entity.Property(x => x.HoTen).HasColumnName("HOTEN").HasColumnType("nvarchar(100)").HasMaxLength(100);
        entity.Property(x => x.TrangThai).HasColumnName("TRANGTHAI");
        entity.Property(x => x.LanDNCuoi).HasColumnName("LANDNCUOI");
        entity.Property(x => x.NgayTao).HasColumnName("NGAYTAO");
        entity.Property(x => x.PhaiDoiMatKhau).HasColumnName("PHAIDOIMATKHAU");
        entity.Property(x => x.MaBaoMat).HasColumnName("MABAOMAT");
    }
}
