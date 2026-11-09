using CampusExam.Application.Accounts;
using CampusExam.Application.Auth;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Application.ThiSinh;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using CampusExam.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CampusExam.Infrastructure.Commands;

/// <summary>Hồ sơ và tài khoản thí sinh theo D-THISINH-01.</summary>
public sealed class EfThiSinhCommandService(
    CampusExamDbContext dbContext,
    IPasswordHasher passwordHasher,
    IMemoryCache cache,
    TimeProvider timeProvider) : IThiSinhCommandService
{
    private const string NhomNhatKy = "THISINH";
    private const string ChiMucCccd = "UX_THISINH_CCCD";

    public async Task<KetQua<ThiSinhChiTietDto>> TaoAsync(
        TaoThiSinhCommand command,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        var msts = command.Msts.Trim();
        var cccd = ChuanHoa(command.Cccd);

        // TENDN = MSTS nên phải trống ở cả hai bảng (tài khoản admin có thể đã dùng tên này).
        if (await dbContext.ThiSinhs.AnyAsync(x => x.Msts == msts, cancellationToken)
            || await dbContext.TaiKhoans.AnyAsync(x => x.TenDN == msts, cancellationToken))
        {
            return TrungMsts(msts);
        }

        if (cccd is not null && await dbContext.ThiSinhs.AnyAsync(x => x.Cccd == cccd, cancellationToken))
        {
            return TrungCccd();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ngaySinh = command.NgaySinh!.Value;
        var taiKhoan = new TaiKhoan
        {
            TenDN = msts,
            MatKhauHash = passwordHasher.Hash(ChinhSachMatKhau.MatKhauMacDinh(ngaySinh)),
            VaiTro = TaiKhoan.VaiTroThiSinh,
            HoTen = command.HoTen.Trim(),
            TrangThai = TaiKhoan.TrangThaiHoatDong,
            NgayTao = now,
            PhaiDoiMatKhau = true,
            MaBaoMat = Guid.NewGuid()
        };
        var thiSinh = new ThiSinh { Msts = msts, NgayTao = now };
        GanHoSo(thiSinh, command, cccd);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.TaiKhoans.Add(taiKhoan);
            await dbContext.SaveChangesAsync(cancellationToken);

            thiSinh.MaTK = taiKhoan.MaTK;
            dbContext.ThiSinhs.Add(thiSinh);
            GhiNhatKy(nguoi, "Thêm thí sinh", msts, null, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlLoi.LaTrungKhoa(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return exception.InnerException?.Message.Contains(ChiMucCccd, StringComparison.Ordinal) == true
                ? TrungCccd()
                : TrungMsts(msts);
        }

        return await ChiTietAsync(msts, cancellationToken);
    }

    public async Task<KetQua<ThiSinhChiTietDto>> SuaAsync(
        string msts,
        SuaThiSinhCommand command,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        var thiSinh = await dbContext.ThiSinhs.SingleOrDefaultAsync(x => x.Msts == msts, cancellationToken);
        if (thiSinh is null)
        {
            return KhongTimThay(msts);
        }

        var cccd = ChuanHoa(command.Cccd);
        if (cccd is not null
            && await dbContext.ThiSinhs.AnyAsync(x => x.Cccd == cccd && x.Msts != thiSinh.Msts, cancellationToken))
        {
            return TrungCccd();
        }

        var taiKhoan = await dbContext.TaiKhoans.SingleAsync(x => x.MaTK == thiSinh.MaTK, cancellationToken);
        GanHoSo(thiSinh, command, cccd);
        taiKhoan.HoTen = thiSinh.HoTen;
        GhiNhatKy(nguoi, "Sửa thí sinh", thiSinh.Msts, null, timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlLoi.LaTrungKhoa(exception))
        {
            dbContext.ChangeTracker.Clear();
            return TrungCccd();
        }

        return await ChiTietAsync(thiSinh.Msts, cancellationToken);
    }

    public async Task<KetQua<ThiSinhChiTietDto>> DoiTrangThaiTaiKhoanAsync(
        string msts,
        TrangThaiTaiKhoan trangThai,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        var taiKhoan = await TimTaiKhoanAsync(msts, cancellationToken);
        if (taiKhoan is null)
        {
            return KhongTimThay(msts);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        taiKhoan.TrangThai = (short)trangThai;
        if (trangThai == TrangThaiTaiKhoan.Khoa)
        {
            taiKhoan.MaBaoMat = Guid.NewGuid();
            await ThuHoiMoiPhienAsync(taiKhoan.MaTK, now, cancellationToken);
        }

        GhiNhatKy(nguoi, trangThai == TrangThaiTaiKhoan.Khoa ? "Khóa tài khoản" : "Mở khóa tài khoản", msts, null, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        cache.Remove(AuthCacheKeys.MaBaoMat(taiKhoan.MaTK));

        return await ChiTietAsync(msts, cancellationToken);
    }

    public async Task<KetQua<ThiSinhChiTietDto>> CapLaiMatKhauAsync(
        string msts,
        NguoiThucHien nguoi,
        CancellationToken cancellationToken)
    {
        var thiSinh = await dbContext.ThiSinhs.AsNoTracking().SingleOrDefaultAsync(x => x.Msts == msts, cancellationToken);
        if (thiSinh is null)
        {
            return KhongTimThay(msts);
        }

        var taiKhoan = await dbContext.TaiKhoans.SingleAsync(x => x.MaTK == thiSinh.MaTK, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Không ghi mật khẩu vào nhật ký; thí sinh tự biết ngày sinh của mình.
        taiKhoan.MatKhauHash = passwordHasher.Hash(ChinhSachMatKhau.MatKhauMacDinh(thiSinh.NgaySinh));
        taiKhoan.PhaiDoiMatKhau = true;
        taiKhoan.MaBaoMat = Guid.NewGuid();
        await ThuHoiMoiPhienAsync(taiKhoan.MaTK, now, cancellationToken);
        GhiNhatKy(nguoi, "Cấp lại mật khẩu", msts, "Đặt lại về ngày sinh", now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        cache.Remove(AuthCacheKeys.MaBaoMat(taiKhoan.MaTK));

        return await ChiTietAsync(msts, cancellationToken);
    }

    public async Task<KetQua<KhongCo>> XoaAsync(string msts, NguoiThucHien nguoi, CancellationToken cancellationToken)
    {
        var thiSinh = await dbContext.ThiSinhs.AsNoTracking().SingleOrDefaultAsync(x => x.Msts == msts, cancellationToken);
        if (thiSinh is null)
        {
            return KhongTimThay(msts);
        }

        if (await dbContext.PhanCongThis.AnyAsync(x => x.Msts == msts, cancellationToken))
        {
            return DangDuocDung();
        }

        var mtk = thiSinh.MaTK;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Giữ nhật ký cũ (đăng nhập, đổi mật khẩu...) nhưng bỏ liên kết tới tài khoản sắp xóa.
            await dbContext.NhatKyHoatDongs.Where(x => x.MaTK == mtk)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaTK, (int?)null), cancellationToken);
            await dbContext.PhienDangNhaps.Where(x => x.MaTK == mtk)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaPhienThay, (long?)null), cancellationToken);
            await dbContext.PhienDangNhaps.Where(x => x.MaTK == mtk).ExecuteDeleteAsync(cancellationToken);
            await dbContext.ThiSinhs.Where(x => x.Msts == msts).ExecuteDeleteAsync(cancellationToken);
            await dbContext.TaiKhoans.Where(x => x.MaTK == mtk).ExecuteDeleteAsync(cancellationToken);

            GhiNhatKy(nguoi, "Xóa thí sinh", msts, thiSinh.HoTen, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (SqlLoi.LaViPhamRangBuoc(exception))
        {
            dbContext.ChangeTracker.Clear();
            // Thí sinh vừa được phân công vào một kỳ thi trong lúc xóa.
            return DangDuocDung();
        }

        cache.Remove(AuthCacheKeys.MaBaoMat(mtk));
        return KetQua<KhongCo>.Ok(default);
    }

    private async Task<KetQua<ThiSinhChiTietDto>> ChiTietAsync(string msts, CancellationToken cancellationToken) =>
        KetQua<ThiSinhChiTietDto>.Ok((await new EfThiSinhQuery(dbContext).GetByMstsAsync(msts, cancellationToken))!);

    private Task<TaiKhoan?> TimTaiKhoanAsync(string msts, CancellationToken cancellationToken) =>
        (from thiSinh in dbContext.ThiSinhs
         join taiKhoan in dbContext.TaiKhoans on thiSinh.MaTK equals taiKhoan.MaTK
         where thiSinh.Msts == msts
         select taiKhoan).SingleOrDefaultAsync(cancellationToken);

    private Task<int> ThuHoiMoiPhienAsync(int mtk, DateTime now, CancellationToken cancellationToken) =>
        dbContext.PhienDangNhaps
            .Where(x => x.MaTK == mtk && x.ThuHoiLuc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, now), cancellationToken);

    private static void GanHoSo(ThiSinh thiSinh, HoSoThiSinhCommand command, string? cccd)
    {
        thiSinh.HoTen = command.HoTen.Trim();
        thiSinh.NgaySinh = command.NgaySinh!.Value;
        thiSinh.GioiTinh = (short?)command.GioiTinh;
        thiSinh.Lop = ChuanHoa(command.Lop);
        thiSinh.NoiSinh = ChuanHoa(command.NoiSinh);
        thiSinh.Cccd = cccd;
    }

    private static string? ChuanHoa(string? giaTri) => string.IsNullOrWhiteSpace(giaTri) ? null : giaTri.Trim();

    private void GhiNhatKy(NguoiThucHien nguoi, string hanhDong, string msts, string? chiTiet, DateTime now) =>
        dbContext.NhatKyHoatDongs.Add(new NhatKyHoatDong
        {
            MaTK = nguoi.MaTK,
            Nhom = NhomNhatKy,
            HanhDong = hanhDong,
            DoiTuong = $"THISINH:{msts}",
            ChiTiet = chiTiet,
            ThoiGian = now,
            DiaChiIP = nguoi.DiaChiIP is { Length: > 45 } ip ? ip[..45] : nguoi.DiaChiIP
        });

    private static LoiNghiepVu KhongTimThay(string msts) => new(
        LoaiLoi.KhongTimThay,
        ErrorCodes.StudentNotFound,
        "Không tìm thấy thí sinh",
        $"Không tồn tại thí sinh có mã số {msts}.");

    private static LoiNghiepVu TrungMsts(string msts) => new(
        LoaiLoi.XungDot,
        ErrorCodes.StudentMstsConflict,
        "Mã số thí sinh đã tồn tại",
        $"Đã có thí sinh hoặc tài khoản dùng mã {msts}.");

    private static LoiNghiepVu TrungCccd() => new(
        LoaiLoi.XungDot,
        ErrorCodes.StudentCccdConflict,
        "CCCD đã tồn tại",
        "Số CCCD này đã gắn với một thí sinh khác.");

    private static LoiNghiepVu DangDuocDung() => new(
        LoaiLoi.XungDot,
        ErrorCodes.StudentInUse,
        "Thí sinh đã được phân công thi",
        "Thí sinh đã có trong kỳ thi nên không xóa được; hãy khóa tài khoản thay vì xóa.");
}
