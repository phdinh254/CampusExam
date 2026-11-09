using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using CampusExam.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Commands;

public sealed class EfKqhtCommandService(
    CampusExamDbContext dbContext) : IKqhtCommandService
{
    public async Task<CreateKqhtResult> CreateAsync(
        CreateKqhtCommand command,
        CancellationToken cancellationToken)
    {
        var kyHieu = command.KyHieu.ToUpperInvariant();

        var daTonTai = await dbContext.KqHocTaps
            .AsNoTracking()
            .AnyAsync(kqht => kqht.KyHieu == kyHieu, cancellationToken);

        if (daTonTai)
        {
            return CreateKqhtResult.Duplicate();
        }

        var kqht = new KqHocTap
        {
            TenKQHT = command.TenKQHT.Trim(),
            ThuTu = command.ThuTu,
            MoTa = ChuanHoaMoTa(command.MoTa),
            TrangThai = (short)command.TrangThai,
            KyHieu = kyHieu
        };

        dbContext.KqHocTaps.Add(kqht);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlLoi.LaTrungKhoa(exception))
        {
            dbContext.Entry(kqht).State = EntityState.Detached;
            return CreateKqhtResult.Duplicate();
        }

        return CreateKqhtResult.Success(new KqhtDto(
            kqht.MaKQHT,
            kqht.TenKQHT,
            kqht.ThuTu,
            kqht.MoTa,
            (TrangThaiSuDung)kqht.TrangThai,
            kqht.KyHieu));
    }

    public async Task<KetQua<KqhtDto>> UpdateAsync(
        int maKQHT,
        UpdateKqhtCommand command,
        CancellationToken cancellationToken)
    {
        var hienTai = await dbContext.KqHocTaps
            .AsNoTracking()
            .Where(kqht => kqht.MaKQHT == maKQHT)
            .Select(kqht => new { kqht.ThuTu })
            .SingleOrDefaultAsync(cancellationToken);

        if (hienTai is null)
        {
            return KhongTimThay(maKQHT);
        }

        var kyHieu = CreateKqhtCommand.KyHieuTheoThuTu(command.ThuTu).ToString();
        var doiThuTu = hienTai.ThuTu != command.ThuTu;

        if (doiThuTu && await dbContext.CauHois.AnyAsync(cauHoi => cauHoi.MaKQHT == maKQHT, cancellationToken))
        {
            return DangDuocDung(
                "KQHT đã có câu hỏi nên không đổi được thứ tự (ký hiệu là một phần của mã câu hỏi).");
        }

        var tenKQHT = command.TenKQHT.Trim();
        var moTa = ChuanHoaMoTa(command.MoTa);

        try
        {
            // ExecuteUpdate: KYHIEU là khóa thay thế nên EF không cho sửa qua entity được theo dõi.
            // Điều kiện "chưa có câu hỏi" nằm trong cùng câu lệnh để không lọt câu hỏi thêm đồng thời;
            // nếu vẫn lọt, khóa ngoại (MAKQHT, KYHIEU) của CAUHOI chặn ở database.
            var soDong = await dbContext.KqHocTaps
                .Where(kqht => kqht.MaKQHT == maKQHT)
                .Where(kqht => !doiThuTu || !dbContext.CauHois.Any(cauHoi => cauHoi.MaKQHT == kqht.MaKQHT))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(kqht => kqht.TenKQHT, tenKQHT)
                        .SetProperty(kqht => kqht.MoTa, moTa)
                        .SetProperty(kqht => kqht.ThuTu, command.ThuTu)
                        .SetProperty(kqht => kqht.KyHieu, kyHieu),
                    cancellationToken);

            if (soDong == 0)
            {
                return await dbContext.KqHocTaps.AnyAsync(kqht => kqht.MaKQHT == maKQHT, cancellationToken)
                    ? DangDuocDung("KQHT vừa có câu hỏi nên không đổi được thứ tự.")
                    : KhongTimThay(maKQHT);
            }
        }
        catch (Exception exception) when (SqlLoi.LaTrungKhoa(exception))
        {
            return TrungKyHieu(kyHieu);
        }
        catch (Exception exception) when (SqlLoi.LaViPhamRangBuoc(exception))
        {
            return DangDuocDung("KQHT vừa có câu hỏi nên không đổi được thứ tự.");
        }

        return KetQua<KqhtDto>.Ok((await new EfKqhtQuery(dbContext).GetByIdAsync(maKQHT, cancellationToken))!);
    }

    public async Task<KetQua<KqhtDto>> DoiTrangThaiAsync(
        int maKQHT,
        TrangThaiSuDung trangThai,
        CancellationToken cancellationToken)
    {
        var soDong = await dbContext.KqHocTaps
            .Where(kqht => kqht.MaKQHT == maKQHT)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(kqht => kqht.TrangThai, (short)trangThai),
                cancellationToken);

        if (soDong == 0)
        {
            return KhongTimThay(maKQHT);
        }

        return KetQua<KqhtDto>.Ok((await new EfKqhtQuery(dbContext).GetByIdAsync(maKQHT, cancellationToken))!);
    }

    public async Task<KetQua<KhongCo>> XoaAsync(
        int maKQHT,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.KqHocTaps.AnyAsync(kqht => kqht.MaKQHT == maKQHT, cancellationToken))
        {
            return KhongTimThay(maKQHT);
        }

        if (await dbContext.CauHois.AnyAsync(cauHoi => cauHoi.MaKQHT == maKQHT, cancellationToken))
        {
            return DangDuocDung("KQHT đã có câu hỏi; hãy chuyển sang ngừng sử dụng thay vì xóa.");
        }

        try
        {
            await dbContext.KqHocTaps
                .Where(kqht => kqht.MaKQHT == maKQHT)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception exception) when (SqlLoi.LaViPhamRangBuoc(exception))
        {
            // Câu hỏi thêm đồng thời hoặc KQHT nằm trong cấu trúc ma trận (CAU_TRUC).
            return DangDuocDung("KQHT đang được câu hỏi hoặc ma trận đề sử dụng; hãy chuyển sang ngừng sử dụng.");
        }

        return KetQua<KhongCo>.Ok(default);
    }

    private static string? ChuanHoaMoTa(string? moTa) =>
        string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();

    private static LoiNghiepVu KhongTimThay(int maKQHT) => new(
        LoaiLoi.KhongTimThay,
        ErrorCodes.KqhtNotFound,
        "Không tìm thấy KQHT",
        $"Không tồn tại KQHT có mã {maKQHT}.");

    private static LoiNghiepVu DangDuocDung(string chiTiet) => new(
        LoaiLoi.XungDot,
        ErrorCodes.KqhtInUse,
        "KQHT đang được sử dụng",
        chiTiet);

    private static LoiNghiepVu TrungKyHieu(string kyHieu) => new(
        LoaiLoi.XungDot,
        ErrorCodes.KqhtKyHieuConflict,
        "Ký hiệu KQHT đã tồn tại",
        $"Đã tồn tại KQHT có ký hiệu {kyHieu}.");
}
