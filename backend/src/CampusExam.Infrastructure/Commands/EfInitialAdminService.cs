using System.Data;
using CampusExam.Application.Accounts;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Commands;

public sealed class EfInitialAdminService(
    CampusExamDbContext dbContext,
    IPasswordHasher passwordHasher) : IInitialAdminService
{
    public async Task<CreateInitialAdminResult> CreateAsync(
        CreateInitialAdminCommand command,
        CancellationToken cancellationToken)
    {
        var tenDN = command.TenDN.Trim().ToLowerInvariant();

        // Serializable: hai lần chạy đồng thời không thể cùng tạo admin đầu tiên.
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (await dbContext.TaiKhoans.AnyAsync(x => x.VaiTro == TaiKhoan.VaiTroAdmin, cancellationToken))
        {
            return CreateInitialAdminResult.Failure(CreateInitialAdminStatus.AdminAlreadyExists);
        }

        if (await dbContext.TaiKhoans.AnyAsync(x => x.TenDN == tenDN, cancellationToken))
        {
            return CreateInitialAdminResult.Failure(CreateInitialAdminStatus.DuplicateTenDN);
        }

        var now = DateTime.UtcNow;
        var admin = new TaiKhoan
        {
            TenDN = tenDN,
            MatKhauHash = passwordHasher.Hash(command.MatKhau),
            VaiTro = TaiKhoan.VaiTroAdmin,
            HoTen = command.HoTen.Trim(),
            TrangThai = TaiKhoan.TrangThaiHoatDong,
            NgayTao = now,
            PhaiDoiMatKhau = false,
            MaBaoMat = Guid.NewGuid()
        };
        dbContext.TaiKhoans.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.NhatKyHoatDongs.Add(new NhatKyHoatDong
        {
            MaTK = admin.MaTK,
            Nhom = "TAIKHOAN",
            HanhDong = "Tạo tài khoản quản trị đầu tiên",
            DoiTuong = $"TAIKHOAN:{tenDN}",
            ThoiGian = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return CreateInitialAdminResult.Success(admin.MaTK);
    }
}
