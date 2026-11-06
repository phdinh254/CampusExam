using CampusExam.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Persistence;

public sealed class CampusExamDbContext : DbContext
{
    public CampusExamDbContext(
        DbContextOptions<CampusExamDbContext> options)
        : base(options)
    {
    }

    public DbSet<MucDo> MucDos => Set<MucDo>();

    public DbSet<KqHocTap> KqHocTaps => Set<KqHocTap>();

    public DbSet<TaiNguyenCauHoi> TaiNguyenCauHois => Set<TaiNguyenCauHoi>();

    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();

    public DbSet<PhienDangNhap> PhienDangNhaps => Set<PhienDangNhap>();

    public DbSet<NhatKyHoatDong> NhatKyHoatDongs => Set<NhatKyHoatDong>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Database lưu UTC; đọc ra gắn Kind = Utc để không bị hiểu nhầm là giờ địa phương.
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("datetime2")
            .HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("datetime2")
            .HaveConversion<UtcNullableDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampusExamDbContext).Assembly);

        modelBuilder.Entity<MucDo>(entity =>
        {
            entity.ToTable("MUCDO", "dbo");

            entity.HasKey(x => x.MaMD);

            entity.Property(x => x.MaMD)
                .HasColumnName("MAMD")
                .ValueGeneratedNever();

            entity.Property(x => x.TenMD)
                .HasColumnName("TENMD")
                .HasMaxLength(30)
                .IsUnicode()
                .IsRequired();
        });

        modelBuilder.Entity<KqHocTap>(entity =>
        {
            entity.ToTable("KQHOCTAP", "dbo");

            entity.HasKey(x => x.MaKQHT)
                .HasName("PK_KQHOCTAP")
                .IsClustered();

            entity.HasAlternateKey(x => x.KyHieu)
                .HasName("AK_AK_KQHOCTAP_KYHIEU_KQHOCTAP");

            entity.Property(x => x.MaKQHT)
                .HasColumnName("MAKQHT")
                .HasColumnType("int")
                .UseIdentityColumn();

            entity.Property(x => x.TenKQHT)
                .HasColumnName("TENKQHT")
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .IsUnicode()
                .IsRequired();

            entity.Property(x => x.ThuTu)
                .HasColumnName("THUTU")
                .HasColumnType("tinyint")
                .IsRequired();

            entity.Property(x => x.MoTa)
                .HasColumnName("MOTA")
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000)
                .IsUnicode();

            entity.Property(x => x.TrangThai)
                .HasColumnName("TRANGTHAI")
                .HasColumnType("smallint")
                .IsRequired();

            entity.Property(x => x.KyHieu)
                .HasColumnName("KYHIEU")
                .HasColumnType("char(1)")
                .HasMaxLength(1)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();
        });

        modelBuilder.Entity<TaiNguyenCauHoi>(entity =>
        {
            entity.ToTable("TAINGUYEN_CAUHOI", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CKT_TAINGUYEN_CAUHOI",
                    "[VITRI] IN ('NOIDUNG','DAA','DAB','DAC','DAD') AND [THUTU] > 0");
                table.HasCheckConstraint(
                    "CKT_TAINGUYEN_CAUHOI_MIMETYPE",
                    "MIMETYPE LIKE 'image/%'");
            });

            entity.HasKey(x => x.MaTN)
                .HasName("PK_TAINGUYEN_CAUHOI")
                .IsClustered();

            entity.HasAlternateKey(x => x.StorageKey)
                .HasName("AK_TAINGUYEN_CAUHOI_STORAGEKEY");

            entity.HasAlternateKey(x => new { x.MaCH, x.ViTri, x.ThuTu })
                .HasName("AK_TAINGUYEN_CAUHOI_VITRI");

            entity.HasIndex(x => x.MaCH)
                .HasDatabaseName("THUOC_CAUHOI_FK");

            entity.Property(x => x.MaTN)
                .HasColumnName("MATN")
                .UseIdentityColumn();

            entity.Property(x => x.MaCH)
                .HasColumnName("MACH")
                .IsRequired();

            entity.Property(x => x.ViTri)
                .HasColumnName("VITRI")
                .HasColumnType("varchar(10)")
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.ThuTu)
                .HasColumnName("THUTU")
                .HasColumnType("smallint")
                .IsRequired();

            entity.Property(x => x.TenFile)
                .HasColumnName("TENFILE")
                .HasColumnType("nvarchar(255)")
                .HasMaxLength(255)
                .IsUnicode()
                .IsRequired();

            entity.Property(x => x.StorageKey)
                .HasColumnName("STORAGEKEY")
                .HasColumnType("varchar(500)")
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.MimeType)
                .HasColumnName("MIMETYPE")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.AltText)
                .HasColumnName("ALTTEXT")
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500)
                .IsUnicode();

            entity.Property(x => x.NgayTao)
                .HasColumnName("NGAYTAO")
                .HasColumnType("datetime2")
                .IsRequired();
        });
    }
}
