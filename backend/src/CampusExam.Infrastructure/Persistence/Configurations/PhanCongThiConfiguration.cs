using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusExam.Infrastructure.Persistence.Configurations;

internal sealed class PhanCongThiConfiguration : IEntityTypeConfiguration<PhanCongThi>
{
    public void Configure(EntityTypeBuilder<PhanCongThi> entity)
    {
        entity.ToTable("PHANCONGTHI", "dbo");

        entity.HasKey(x => x.MaPhanCong).HasName("PK_PHANCONGTHI");
        entity.HasIndex(x => x.Msts).HasDatabaseName("PHAN_CONG_THI_SINH_FK");

        entity.Property(x => x.MaPhanCong).HasColumnName("MAPHANCONG").UseIdentityColumn();
        entity.Property(x => x.MaKT).HasColumnName("MAKT");
        entity.Property(x => x.Msts).HasColumnName("MSTS").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false);
    }
}

internal sealed class BaiLamConfiguration : IEntityTypeConfiguration<BaiLam>
{
    public void Configure(EntityTypeBuilder<BaiLam> entity)
    {
        entity.ToTable("BAILAM", "dbo");

        entity.HasKey(x => x.MaBL).HasName("PK_BAILAM");

        entity.Property(x => x.MaBL).HasColumnName("MABL").UseIdentityColumn();
        entity.Property(x => x.MaPhanCong).HasColumnName("MAPHANCONG");
    }
}
