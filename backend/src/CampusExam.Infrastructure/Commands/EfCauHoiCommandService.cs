using System.Data;
using CampusExam.Application.CauHoi;
using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using CampusExam.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Commands;

public sealed class EfCauHoiCommandService(
    CampusExamDbContext dbContext,
    TimeProvider timeProvider,
    IFileStorage fileStorage) : ICauHoiCommandService
{
    /// <summary>Số lần thử lại khi hai request cùng lấy một STT (hiếm vì đã khóa phạm vi).</summary>
    private const int SoLanThuCapStt = 3;

    public async Task<KetQua<CauHoiChiTietDto>> TaoAsync(
        TaoCauHoiCommand command,
        CancellationToken cancellationToken)
    {
        var kqht = await dbContext.KqHocTaps
            .AsNoTracking()
            .Where(x => x.MaKQHT == command.MaKQHT)
            .Select(x => new { x.KyHieu, x.TrangThai })
            .SingleOrDefaultAsync(cancellationToken);

        if (kqht is null)
        {
            return new LoiNghiepVu(
                LoaiLoi.KhongHopLe,
                ErrorCodes.ValidationFailed,
                "Dữ liệu không hợp lệ",
                Truong: "maKQHT",
                ChiTiet: "KQHT không tồn tại.");
        }

        if (kqht.TrangThai != (short)TrangThaiSuDung.DangSuDung)
        {
            return new LoiNghiepVu(
                LoaiLoi.XungDot,
                ErrorCodes.KqhtInactive,
                "KQHT đã ngừng sử dụng",
                "Không thêm được câu hỏi vào KQHT đã ngừng sử dụng.");
        }

        for (var lan = 1; ; lan++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            // UPDLOCK + HOLDLOCK khóa phạm vi (KQHT, mức độ) tới hết transaction,
            // nên hai request đồng thời không cùng lấy một STT.
            var stt = (await dbContext.Database
                .SqlQuery<int>($"""
                    SELECT ISNULL(MAX(STT), 0) + 1 AS [Value]
                    FROM dbo.CAUHOI WITH (UPDLOCK, HOLDLOCK)
                    WHERE MAKQHT = {command.MaKQHT} AND MAMD = {command.MaMD}
                    """)
                .ToListAsync(cancellationToken)).Single();

            var cauHoi = new CauHoi
            {
                MaKQHT = command.MaKQHT,
                MaMD = command.MaMD,
                KyHieu = kqht.KyHieu,
                Stt = stt,
                MaCauHoi = CauHoi.TaoMaCauHoi(kqht.KyHieu, command.MaMD, stt),
                TrangThai = CauHoi.TrangThaiDangSuDung,
                NgayTao = timeProvider.GetUtcNow().UtcDateTime
            };
            GanNoiDung(cauHoi, command);
            dbContext.CauHois.Add(cauHoi);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return KetQua<CauHoiChiTietDto>.Ok((await TaiChiTietAsync(cauHoi.MaCH, cancellationToken))!);
            }
            catch (DbUpdateException exception) when (SqlLoi.LaTrungKhoa(exception) && lan < SoLanThuCapStt)
            {
                dbContext.Entry(cauHoi).State = EntityState.Detached;
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }

    public async Task<KetQua<CauHoiChiTietDto>> SuaAsync(
        int maCH,
        SuaCauHoiCommand command,
        CancellationToken cancellationToken)
    {
        var cauHoi = await dbContext.CauHois.SingleOrDefaultAsync(x => x.MaCH == maCH, cancellationToken);
        if (cauHoi is null)
        {
            return KhongTimThay(maCH);
        }

        // Phiên bản client đã xem là giá trị gốc: lệch với database thì UPDATE không khớp dòng nào.
        dbContext.Entry(cauHoi).Property(x => x.PhienBan).OriginalValue = command.PhienBanBytes()!;
        GanNoiDung(cauHoi, command);
        cauHoi.NgayCapNhat = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.Entry(cauHoi).State = EntityState.Detached;
            return new LoiNghiepVu(
                LoaiLoi.XungDot,
                ErrorCodes.ConcurrencyConflict,
                "Câu hỏi đã bị thay đổi",
                "Câu hỏi vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất trước khi sửa.");
        }

        return KetQua<CauHoiChiTietDto>.Ok((await TaiChiTietAsync(maCH, cancellationToken))!);
    }

    public async Task<KetQua<CauHoiChiTietDto>> DoiTrangThaiAsync(
        int maCH,
        TrangThaiSuDung trangThai,
        CancellationToken cancellationToken)
    {
        var bayGio = timeProvider.GetUtcNow().UtcDateTime;
        var soDong = await dbContext.CauHois
            .Where(x => x.MaCH == maCH)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.TrangThai, (short)trangThai)
                    .SetProperty(x => x.NgayCapNhat, bayGio),
                cancellationToken);

        if (soDong == 0)
        {
            return KhongTimThay(maCH);
        }

        return KetQua<CauHoiChiTietDto>.Ok((await TaiChiTietAsync(maCH, cancellationToken))!);
    }

    public async Task<KetQua<KhongCo>> XoaAsync(int maCH, CancellationToken cancellationToken)
    {
        if (!await dbContext.CauHois.AnyAsync(x => x.MaCH == maCH, cancellationToken))
        {
            return KhongTimThay(maCH);
        }

        if (await dbContext.GomCaus.AnyAsync(x => x.MaCH == maCH, cancellationToken))
        {
            return DangDuocDung();
        }

        var keyHinh = await dbContext.TaiNguyenCauHois
            .Where(x => x.MaCH == maCH)
            .Select(x => x.StorageKey)
            .ToListAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.TaiNguyenCauHois.Where(x => x.MaCH == maCH).ExecuteDeleteAsync(cancellationToken);
            await dbContext.CauHois.Where(x => x.MaCH == maCH).ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (SqlLoi.LaViPhamRangBuoc(exception))
        {
            // Câu hỏi vừa được đưa vào một đề trong lúc xóa.
            return DangDuocDung();
        }

        // Xóa file sau khi commit: lỗi xóa file chỉ để lại file mồ côi, không mất dữ liệu.
        foreach (var key in keyHinh)
        {
            await fileStorage.DeleteAsync(key, CancellationToken.None);
        }

        return KetQua<KhongCo>.Ok(default);
    }

    private Task<CauHoiChiTietDto?> TaiChiTietAsync(int maCH, CancellationToken cancellationToken) =>
        new EfCauHoiQuery(dbContext).GetByIdAsync(maCH, cancellationToken);

    private static void GanNoiDung(CauHoi cauHoi, NoiDungCauHoiCommand command)
    {
        cauHoi.NoiDung = command.NoiDung.Trim();
        cauHoi.DaA = command.PhuongAnA.Trim();
        cauHoi.DaB = command.PhuongAnB.Trim();
        cauHoi.DaC = command.PhuongAnC.Trim();
        cauHoi.DaD = command.PhuongAnD.Trim();
        cauHoi.DapAnDung = command.DapAnDung.ToUpperInvariant();
        cauHoi.CoDinhDa = command.CoDinhDapAn ? (short)1 : (short)0;
    }

    private static LoiNghiepVu KhongTimThay(int maCH) => new(
        LoaiLoi.KhongTimThay,
        ErrorCodes.QuestionNotFound,
        "Không tìm thấy câu hỏi",
        $"Không tồn tại câu hỏi có mã {maCH}.");

    private static LoiNghiepVu DangDuocDung() => new(
        LoaiLoi.XungDot,
        ErrorCodes.QuestionInUse,
        "Câu hỏi đã nằm trong đề thi",
        "Câu hỏi đã được dùng trong đề nên không xóa được; hãy chuyển sang ngừng sử dụng.");
}
