using CampusExam.Application.CauHoi;
using CampusExam.Application.Common.Models;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Queries;

public sealed class EfCauHoiQuery(
    CampusExamDbContext dbContext) : ICauHoiQuery
{
    /// <summary>Số ký tự nội dung hiển thị ở cột tóm tắt của bảng câu hỏi.</summary>
    public const int DoDaiTomTat = 160;

    public async Task<TrangDuLieu<CauHoiTomTatDto>> GetPageAsync(
        TruyVanCauHoi truyVan,
        CancellationToken cancellationToken)
    {
        var nguon =
            from cauHoi in dbContext.CauHois.AsNoTracking()
            join kqht in dbContext.KqHocTaps.AsNoTracking() on cauHoi.MaKQHT equals kqht.MaKQHT
            select new { CauHoi = cauHoi, Kqht = kqht };

        var tuKhoa = truyVan.TuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
        {
            nguon = nguon.Where(x => x.CauHoi.MaCauHoi.Contains(tuKhoa) || x.CauHoi.NoiDung.Contains(tuKhoa));
        }

        if (truyVan.MaKQHT is { } maKQHT)
        {
            nguon = nguon.Where(x => x.CauHoi.MaKQHT == maKQHT);
        }

        if (truyVan.MaMD is { } maMD)
        {
            nguon = nguon.Where(x => x.CauHoi.MaMD == maMD);
        }

        if (truyVan.TrangThaiEnum is { } trangThai)
        {
            var giaTri = (short)trangThai;
            nguon = nguon.Where(x => x.CauHoi.TrangThai == giaTri);
        }

        var tongSoDong = await nguon.CountAsync(cancellationToken);

        var sapXep = truyVan.SapXep ?? TruyVanCauHoi.SapXepMacDinh;
        var giam = sapXep.StartsWith('-');
        var daSapXep = sapXep.TrimStart('-') switch
        {
            // Theo (KQHT, mức độ, STT) thay vì chuỗi mã để A1-2 đứng trước A1-10.
            "maCauHoi" => giam
                ? nguon.OrderByDescending(x => x.Kqht.ThuTu).ThenByDescending(x => x.CauHoi.MaMD).ThenByDescending(x => x.CauHoi.Stt)
                : nguon.OrderBy(x => x.Kqht.ThuTu).ThenBy(x => x.CauHoi.MaMD).ThenBy(x => x.CauHoi.Stt),
            "ngayTao" => giam
                ? nguon.OrderByDescending(x => x.CauHoi.NgayTao)
                : nguon.OrderBy(x => x.CauHoi.NgayTao),
            _ => giam
                ? nguon.OrderByDescending(x => x.CauHoi.NgayCapNhat ?? x.CauHoi.NgayTao)
                : nguon.OrderBy(x => x.CauHoi.NgayCapNhat ?? x.CauHoi.NgayTao)
        };

        var dong = await daSapXep
            .ThenByDescending(x => x.CauHoi.MaCH)
            .Skip(truyVan.SoDongBoQua)
            .Take(truyVan.KichThuocTrang)
            .Select(x => new
            {
                x.CauHoi.MaCH,
                x.CauHoi.MaCauHoi,
                TomTat = x.CauHoi.NoiDung.Length > DoDaiTomTat
                    ? x.CauHoi.NoiDung.Substring(0, DoDaiTomTat)
                    : x.CauHoi.NoiDung,
                DaiHon = x.CauHoi.NoiDung.Length > DoDaiTomTat,
                x.Kqht.MaKQHT,
                x.Kqht.ThuTu,
                x.Kqht.KyHieu,
                x.CauHoi.MaMD,
                x.CauHoi.TrangThai,
                CoHinhAnh = dbContext.TaiNguyenCauHois.Any(tn => tn.MaCH == x.CauHoi.MaCH),
                x.CauHoi.NgayTao,
                x.CauHoi.NgayCapNhat
            })
            .ToListAsync(cancellationToken);

        var duLieu = dong
            .Select(x => new CauHoiTomTatDto(
                x.MaCH,
                x.MaCauHoi,
                x.DaiHon ? x.TomTat.TrimEnd() + "…" : x.TomTat,
                new KqhtTomTatDto(x.MaKQHT, x.ThuTu, x.KyHieu),
                x.MaMD,
                (TrangThaiSuDung)x.TrangThai,
                x.CoHinhAnh,
                x.NgayCapNhat ?? x.NgayTao))
            .ToList();

        return new TrangDuLieu<CauHoiTomTatDto>(duLieu, truyVan.Trang, truyVan.KichThuocTrang, tongSoDong);
    }

    public async Task<ThongKeCauHoiDto> GetThongKeAsync(CancellationToken cancellationToken)
    {
        var theoTrangThai = await dbContext.CauHois
            .AsNoTracking()
            .GroupBy(cauHoi => cauHoi.TrangThai)
            .Select(nhom => new { TrangThai = nhom.Key, SoLuong = nhom.Count() })
            .ToListAsync(cancellationToken);

        var dangSuDung = theoTrangThai.Where(x => x.TrangThai == CauHoi.TrangThaiDangSuDung).Sum(x => x.SoLuong);
        var ngungSuDung = theoTrangThai.Where(x => x.TrangThai == CauHoi.TrangThaiNgungSuDung).Sum(x => x.SoLuong);

        var soKQHT = await dbContext.KqHocTaps
            .AsNoTracking()
            .CountAsync(kqht => kqht.TrangThai == (short)TrangThaiSuDung.DangSuDung, cancellationToken);

        return new ThongKeCauHoiDto(dangSuDung + ngungSuDung, dangSuDung, ngungSuDung, soKQHT);
    }

    public Task<HinhAnhLuuTru?> GetHinhAnhAsync(int maCH, int maTN, CancellationToken cancellationToken) =>
        dbContext.TaiNguyenCauHois
            .AsNoTracking()
            .Where(tn => tn.MaCH == maCH && tn.MaTN == maTN)
            .Select(tn => new HinhAnhLuuTru(tn.StorageKey, tn.MimeType, tn.TenFile))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CauHoiChiTietDto?> GetByIdAsync(int maCH, CancellationToken cancellationToken)
    {
        var cauHoi = await dbContext.CauHois
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaCH == maCH, cancellationToken);

        if (cauHoi is null)
        {
            return null;
        }

        var hinhAnh = await dbContext.TaiNguyenCauHois
            .AsNoTracking()
            .Where(tn => tn.MaCH == maCH)
            .OrderBy(tn => tn.ViTri)
            .ThenBy(tn => tn.ThuTu)
            .Select(tn => new HinhAnhCauHoiDto(
                tn.MaTN,
                tn.ViTri,
                tn.ThuTu,
                "/api/cau-hoi/" + maCH + "/hinh-anh/" + tn.MaTN,
                tn.AltText))
            .ToListAsync(cancellationToken);

        var soDeDaDung = await dbContext.GomCaus
            .AsNoTracking()
            .CountAsync(gc => gc.MaCH == maCH, cancellationToken);

        return new CauHoiChiTietDto(
            cauHoi.MaCH,
            cauHoi.MaCauHoi,
            cauHoi.MaKQHT,
            cauHoi.MaMD,
            cauHoi.NoiDung,
            cauHoi.DaA,
            cauHoi.DaB,
            cauHoi.DaC,
            cauHoi.DaD,
            cauHoi.DapAnDung,
            cauHoi.CoDinhDa == 1,
            (TrangThaiSuDung)cauHoi.TrangThai,
            hinhAnh,
            soDeDaDung,
            Convert.ToBase64String(cauHoi.PhienBan),
            cauHoi.NgayTao,
            cauHoi.NgayCapNhat);
    }
}
