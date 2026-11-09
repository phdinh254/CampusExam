using CampusExam.Application.Common.Models;
using CampusExam.Application.ThiSinh;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Queries;

public sealed class EfThiSinhQuery(CampusExamDbContext dbContext) : IThiSinhQuery
{
    public async Task<TrangDuLieu<ThiSinhTomTatDto>> GetPageAsync(TruyVanThiSinh truyVan, CancellationToken cancellationToken)
    {
        var nguon =
            from thiSinh in dbContext.ThiSinhs.AsNoTracking()
            join taiKhoan in dbContext.TaiKhoans.AsNoTracking() on thiSinh.MaTK equals taiKhoan.MaTK
            select new { ThiSinh = thiSinh, TaiKhoan = taiKhoan };

        var tuKhoa = truyVan.TuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
        {
            nguon = nguon.Where(x => x.ThiSinh.Msts.Contains(tuKhoa) || x.ThiSinh.HoTen.Contains(tuKhoa));
        }

        var lop = truyVan.Lop?.Trim();
        if (!string.IsNullOrEmpty(lop))
        {
            nguon = nguon.Where(x => x.ThiSinh.Lop == lop);
        }

        if (truyVan.TrangThaiEnum is { } trangThai)
        {
            var giaTri = (short)trangThai;
            nguon = nguon.Where(x => x.TaiKhoan.TrangThai == giaTri);
        }

        if (truyVan.DaDoiMatKhau is { } daDoi)
        {
            nguon = nguon.Where(x => x.TaiKhoan.PhaiDoiMatKhau == !daDoi);
        }

        var tongSoDong = await nguon.CountAsync(cancellationToken);

        var sapXep = truyVan.SapXep ?? TruyVanThiSinh.SapXepMacDinh;
        var giam = sapXep.StartsWith('-');
        var daSapXep = sapXep.TrimStart('-') switch
        {
            "hoTen" => giam ? nguon.OrderByDescending(x => x.ThiSinh.HoTen) : nguon.OrderBy(x => x.ThiSinh.HoTen),
            "lop" => giam ? nguon.OrderByDescending(x => x.ThiSinh.Lop) : nguon.OrderBy(x => x.ThiSinh.Lop),
            "ngayTao" => giam ? nguon.OrderByDescending(x => x.ThiSinh.NgayTao) : nguon.OrderBy(x => x.ThiSinh.NgayTao),
            _ => giam ? nguon.OrderByDescending(x => x.ThiSinh.Msts) : nguon.OrderBy(x => x.ThiSinh.Msts)
        };

        var dong = await daSapXep
            .ThenBy(x => x.ThiSinh.Msts)
            .Skip(truyVan.SoDongBoQua)
            .Take(truyVan.KichThuocTrang)
            .Select(x => new
            {
                x.ThiSinh.Msts,
                x.ThiSinh.HoTen,
                x.ThiSinh.NgaySinh,
                x.ThiSinh.GioiTinh,
                x.ThiSinh.Lop,
                x.TaiKhoan.TrangThai,
                x.TaiKhoan.PhaiDoiMatKhau,
                SoLuotThi = (
                    from phanCong in dbContext.PhanCongThis
                    join baiLam in dbContext.BaiLams on phanCong.MaPhanCong equals baiLam.MaPhanCong
                    where phanCong.Msts == x.ThiSinh.Msts
                    select baiLam.MaBL).Count(),
                x.ThiSinh.NgayTao
            })
            .ToListAsync(cancellationToken);

        var duLieu = dong
            .Select(x => new ThiSinhTomTatDto(
                x.Msts,
                x.HoTen,
                x.NgaySinh,
                (GioiTinh?)x.GioiTinh,
                x.Lop,
                (TrangThaiTaiKhoan)x.TrangThai,
                !x.PhaiDoiMatKhau,
                x.SoLuotThi,
                x.NgayTao))
            .ToList();

        return new TrangDuLieu<ThiSinhTomTatDto>(duLieu, truyVan.Trang, truyVan.KichThuocTrang, tongSoDong);
    }

    public async Task<IReadOnlyList<string>> GetLopAsync(CancellationToken cancellationToken) =>
        await dbContext.ThiSinhs
            .AsNoTracking()
            .Where(x => x.Lop != null && x.Lop != "")
            .Select(x => x.Lop!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<ThiSinhChiTietDto?> GetByMstsAsync(string msts, CancellationToken cancellationToken)
    {
        var x = await (
                from thiSinh in dbContext.ThiSinhs.AsNoTracking()
                join taiKhoan in dbContext.TaiKhoans.AsNoTracking() on thiSinh.MaTK equals taiKhoan.MaTK
                where thiSinh.Msts == msts
                select new { ThiSinh = thiSinh, TaiKhoan = taiKhoan })
            .SingleOrDefaultAsync(cancellationToken);

        if (x is null)
        {
            return null;
        }

        var soLanPhanCong = await dbContext.PhanCongThis.CountAsync(pc => pc.Msts == x.ThiSinh.Msts, cancellationToken);
        var soLuotThi = await (
                from phanCong in dbContext.PhanCongThis
                join baiLam in dbContext.BaiLams on phanCong.MaPhanCong equals baiLam.MaPhanCong
                where phanCong.Msts == x.ThiSinh.Msts
                select baiLam.MaBL)
            .CountAsync(cancellationToken);

        return new ThiSinhChiTietDto(
            x.ThiSinh.Msts,
            x.ThiSinh.HoTen,
            x.ThiSinh.NgaySinh,
            (GioiTinh?)x.ThiSinh.GioiTinh,
            x.ThiSinh.Lop,
            x.ThiSinh.NoiSinh,
            x.ThiSinh.Cccd,
            (TrangThaiTaiKhoan)x.TaiKhoan.TrangThai,
            !x.TaiKhoan.PhaiDoiMatKhau,
            x.TaiKhoan.LanDNCuoi,
            soLuotThi,
            soLanPhanCong,
            x.ThiSinh.NgayTao);
    }
}
