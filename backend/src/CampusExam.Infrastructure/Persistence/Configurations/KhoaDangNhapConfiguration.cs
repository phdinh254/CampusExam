using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class KhoaDangNhapConfiguration : IEntityTypeConfiguration<KhoaDangNhap>
{
    public void Configure(EntityTypeBuilder<KhoaDangNhap> entity)
    {
        entity.ToTable("KHOADANGNHAP", "dbo");

        entity.HasKey(x => new { x.TenDN, x.DiaChiIP }).HasName("PK_KHOADANGNHAP");
        entity.HasIndex(x => x.LanSaiCuoi).HasDatabaseName("IX_KHOADANGNHAP_LANSAICUOI");

        entity.Property(x => x.TenDN).HasColumnName("TENDN").HasColumnType("varchar(50)").HasMaxLength(50).IsUnicode(false);
        entity.Property(x => x.DiaChiIP).HasColumnName("DIACHIIP").HasColumnType("varchar(45)").HasMaxLength(45).IsUnicode(false);
        entity.Property(x => x.SoLanSai).HasColumnName("SOLANSAI");
        entity.Property(x => x.KhoaDen).HasColumnName("KHOADEN");
        entity.Property(x => x.LanSaiCuoi).HasColumnName("LANSAICUOI");
    }
}
