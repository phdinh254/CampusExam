using System.Data;
using System.Security.Cryptography;
using System.Text;
using CampusExam.Application.Accounts;
using CampusExam.Application.Auth;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CampusExam.Infrastructure.Services;

/// <summary>
/// Đăng nhập, phiên (refresh token xoay vòng) và đổi mật khẩu theo docs/API_DESIGN.md mục 1.5
/// và D-THISINH-01.
/// </summary>
public sealed class EfAuthService(
    CampusExamDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    IMemoryCache cache) : IAuthService
{
    private const string NhomNhatKy = "XACTHUC";

    /// <summary>Khoảng thời gian một token vừa bị xoay vòng còn được coi là tranh chấp hợp lệ (nhiều tab).</summary>
    private static readonly TimeSpan KhoangAnHoaXoayVong = TimeSpan.FromSeconds(30);

    /// <summary>Hash giả để tên đăng nhập không tồn tại vẫn tốn thời gian như khi sai mật khẩu.</summary>
    private static string? s_hashGia;

    private readonly AuthOptions _options = options.Value;

    private DateTime BayGio => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<DangNhapResult> DangNhapAsync(
        DangNhapRequest request,
        NguCanhYeuCau nguCanh,
        CancellationToken cancellationToken)
    {
        var tenDN = request.TenDN.Trim();
        var now = BayGio;
        var khoa = new KhoaCap(tenDN, nguCanh);

        // Kiểm tra trước khi tìm tài khoản: tên có thật hay không đều trả 423 giống nhau.
        if (await LayKhoaDenAsync(khoa, now, cancellationToken) is { } dangKhoa)
        {
            return new DangNhapResult(KetQuaDangNhap.TamKhoa, KhoaDen: dangKhoa);
        }

        var taiKhoan = await dbContext.TaiKhoans.SingleOrDefaultAsync(x => x.TenDN == tenDN, cancellationToken);

        if (taiKhoan is null)
        {
            s_hashGia ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            passwordHasher.Verify(s_hashGia, request.MatKhau);
            var khoaDen = await GhiLanSaiAsync(null, khoa, tenDN, "Đăng nhập thất bại",
                "Tên đăng nhập không tồn tại", nguCanh, now, cancellationToken);
            return khoaDen is null
                ? new DangNhapResult(KetQuaDangNhap.SaiThongTin)
                : new DangNhapResult(KetQuaDangNhap.TamKhoa, KhoaDen: khoaDen);
        }

        var kiemTra = passwordHasher.Verify(taiKhoan.MatKhauHash, request.MatKhau);
        if (kiemTra == PasswordCheck.Failed)
        {
            var khoaDen = await GhiLanSaiAsync(taiKhoan.MaTK, khoa, taiKhoan.TenDN, "Đăng nhập thất bại",
                "Sai mật khẩu", nguCanh, now, cancellationToken);
            return khoaDen is null
                ? new DangNhapResult(KetQuaDangNhap.SaiThongTin)
                : new DangNhapResult(KetQuaDangNhap.TamKhoa, KhoaDen: khoaDen);
        }

        if (taiKhoan.TrangThai != TaiKhoan.TrangThaiHoatDong)
        {
            GhiNhatKy(taiKhoan.MaTK, "Đăng nhập thất bại", taiKhoan.TenDN, "Tài khoản đã bị khóa", nguCanh, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DangNhapResult(KetQuaDangNhap.BiVoHieuHoa);
        }

        if (kiemTra == PasswordCheck.SuccessRehashNeeded)
        {
            taiKhoan.MatKhauHash = passwordHasher.Hash(request.MatKhau);
        }

        await XoaLanSaiAsync(khoa, cancellationToken);
        taiKhoan.LanDNCuoi = now;
        GhiNhatKy(taiKhoan.MaTK, "Đăng nhập", taiKhoan.TenDN, null, nguCanh, now);

        var phien = await CapPhienAsync(taiKhoan, request.GhiNho, _options.ThoiHanPhien(request.GhiNho), nguCanh, now, cancellationToken);
        return new DangNhapResult(KetQuaDangNhap.ThanhCong, phien.Phien);
    }

    public async Task<PhienDaCap?> LamMoiAsync(
        string refreshToken,
        NguCanhYeuCau nguCanh,
        CancellationToken cancellationToken)
    {
        var now = BayGio;
        var hash = HashToken(refreshToken);
        var phienCu = await dbContext.PhienDangNhaps.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (phienCu is null)
        {
            return null;
        }

        if (phienCu.ThuHoiLuc is { } thuHoiLuc)
        {
            // Token đã bị thay mà vẫn được dùng lại ngoài khoảng tranh chấp giữa các tab: coi là bị đánh cắp.
            if (phienCu.MaPhienThay is not null && now - thuHoiLuc > KhoangAnHoaXoayVong)
            {
                await ThuHoiMoiPhienAsync(phienCu.MaTK, now, cancellationToken);
                GhiNhatKy(phienCu.MaTK, "Thu hồi mọi phiên", $"TAIKHOAN:{phienCu.MaTK}",
                    "Refresh token đã xoay vòng bị dùng lại", nguCanh, now);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        if (phienCu.HetHan <= now)
        {
            return null;
        }

        var taiKhoan = await dbContext.TaiKhoans.SingleAsync(x => x.MaTK == phienCu.MaTK, cancellationToken);
        if (taiKhoan.TrangThai != TaiKhoan.TrangThaiHoatDong)
        {
            await ThuHoiMoiPhienAsync(taiKhoan.MaTK, now, cancellationToken);
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Cập nhật có điều kiện: hai request cùng token thì chỉ một request thắng.
        var daThuHoi = await dbContext.PhienDangNhaps
            .Where(x => x.MaPhien == phienCu.MaPhien && x.ThuHoiLuc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, now), cancellationToken);
        if (daThuHoi == 0)
        {
            return null;
        }

        var thoiHan = phienCu.HetHan - phienCu.NgayTao;
        var ghiNho = thoiHan > TimeSpan.FromHours(_options.RefreshTokenHours);
        var moi = await CapPhienAsync(taiKhoan, ghiNho, thoiHan, nguCanh, now, cancellationToken);

        await dbContext.PhienDangNhaps
            .Where(x => x.MaPhien == phienCu.MaPhien)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaPhienThay, moi.MaPhien), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return moi.Phien;
    }

    public async Task DangXuatAsync(string refreshToken, NguCanhYeuCau nguCanh, CancellationToken cancellationToken)
    {
        var now = BayGio;
        var hash = HashToken(refreshToken);
        var phien = await dbContext.PhienDangNhaps
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.ThuHoiLuc == null, cancellationToken);
        if (phien is null)
        {
            return;
        }

        phien.ThuHoiLuc = now;
        GhiNhatKy(phien.MaTK, "Đăng xuất", $"TAIKHOAN:{phien.MaTK}", null, nguCanh, now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaiKhoanHienTaiDto?> LayTaiKhoanAsync(int mtk, CancellationToken cancellationToken)
    {
        var taiKhoan = await dbContext.TaiKhoans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaTK == mtk, cancellationToken);
        if (taiKhoan is null)
        {
            return null;
        }

        var thiSinh = await TimThiSinhAsync(taiKhoan, cancellationToken);
        return ToDto(taiKhoan, thiSinh);
    }

    public async Task<DoiMatKhauResult> DoiMatKhauAsync(
        int mtk,
        DoiMatKhauRequest request,
        string? refreshTokenHienTai,
        NguCanhYeuCau nguCanh,
        CancellationToken cancellationToken)
    {
        var now = BayGio;
        var taiKhoan = await dbContext.TaiKhoans.SingleAsync(x => x.MaTK == mtk, cancellationToken);

        // Dùng chung bộ đếm với đăng nhập: chặn dò mật khẩu bằng access token bị lộ.
        var khoa = new KhoaCap(taiKhoan.TenDN, nguCanh);
        if (await LayKhoaDenAsync(khoa, now, cancellationToken) is { } dangKhoa)
        {
            return new DoiMatKhauResult(KetQuaDoiMatKhau.TamKhoa, KhoaDen: dangKhoa);
        }

        if (passwordHasher.Verify(taiKhoan.MatKhauHash, request.MatKhauCu) == PasswordCheck.Failed)
        {
            var khoaDen = await GhiLanSaiAsync(mtk, khoa, taiKhoan.TenDN, "Đổi mật khẩu thất bại",
                "Sai mật khẩu hiện tại", nguCanh, now, cancellationToken);
            return khoaDen is null
                ? new DoiMatKhauResult(KetQuaDoiMatKhau.SaiMatKhauCu)
                : new DoiMatKhauResult(KetQuaDoiMatKhau.TamKhoa, KhoaDen: khoaDen);
        }

        await XoaLanSaiAsync(khoa, cancellationToken);

        var thiSinh = await TimThiSinhAsync(taiKhoan, cancellationToken);
        var loi = ChinhSachMatKhau.KiemTra(request.MatKhauMoi, taiKhoan.TenDN, thiSinh?.NgaySinh).ToList();
        if (request.MatKhauMoi == request.MatKhauCu)
        {
            loi.Add("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        if (loi.Count > 0)
        {
            return new DoiMatKhauResult(KetQuaDoiMatKhau.MatKhauMoiKhongHopLe, Loi: loi);
        }

        var ghiNho = false;
        if (!string.IsNullOrEmpty(refreshTokenHienTai))
        {
            var hash = HashToken(refreshTokenHienTai);
            var phienHienTai = await dbContext.PhienDangNhaps.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TokenHash == hash && x.MaTK == mtk, cancellationToken);
            ghiNho = phienHienTai is not null
                     && phienHienTai.HetHan - phienHienTai.NgayTao > TimeSpan.FromHours(_options.RefreshTokenHours);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        taiKhoan.MatKhauHash = passwordHasher.Hash(request.MatKhauMoi);
        taiKhoan.PhaiDoiMatKhau = false;
        taiKhoan.MaBaoMat = Guid.NewGuid(); // vô hiệu mọi access token đã cấp
        await ThuHoiMoiPhienAsync(mtk, now, cancellationToken);
        GhiNhatKy(mtk, "Đổi mật khẩu", taiKhoan.TenDN, null, nguCanh, now);

        var phien = await CapPhienAsync(taiKhoan, ghiNho, _options.ThoiHanPhien(ghiNho), nguCanh, now, cancellationToken, thiSinh);
        await transaction.CommitAsync(cancellationToken);
        cache.Remove(AuthCacheKeys.MaBaoMat(mtk)); // token cũ bị từ chối ngay, không chờ hết hạn cache

        return new DoiMatKhauResult(KetQuaDoiMatKhau.ThanhCong, phien.Phien);
    }

    public async Task<bool> AccessTokenConHieuLucAsync(int mtk, Guid maBaoMat, CancellationToken cancellationToken)
    {
        var khoaCache = AuthCacheKeys.MaBaoMat(mtk);
        if (_options.TokenCheckCacheSeconds > 0 && cache.TryGetValue(khoaCache, out Guid? daCache))
        {
            return daCache == maBaoMat;
        }

        // Mã bảo mật hiện tại của tài khoản còn hoạt động; null nếu đã khóa hoặc không tồn tại.
        var hienTai = await dbContext.TaiKhoans.AsNoTracking()
            .Where(x => x.MaTK == mtk && x.TrangThai == TaiKhoan.TrangThaiHoatDong)
            .Select(x => (Guid?)x.MaBaoMat)
            .SingleOrDefaultAsync(cancellationToken);

        if (_options.TokenCheckCacheSeconds > 0)
        {
            cache.Set(khoaCache, hienTai, TimeSpan.FromSeconds(_options.TokenCheckCacheSeconds));
        }

        return hienTai == maBaoMat;
    }

    /// <summary>Tạo phiên mới, lưu mọi thay đổi đang chờ trong DbContext và trả token.</summary>
    private async Task<(PhienDaCap Phien, long MaPhien)> CapPhienAsync(
        TaiKhoan taiKhoan,
        bool ghiNho,
        TimeSpan thoiHan,
        NguCanhYeuCau nguCanh,
        DateTime now,
        CancellationToken cancellationToken,
        ThiSinh? thiSinh = null)
    {
        thiSinh ??= await TimThiSinhAsync(taiKhoan, cancellationToken);

        var refreshToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var phien = new PhienDangNhap
        {
            MaTK = taiKhoan.MaTK,
            TokenHash = HashToken(refreshToken),
            NgayTao = now,
            HetHan = now + thoiHan,
            DiaChiIP = Cat(nguCanh.DiaChiIP, 45),
            TrinhDuyet = Cat(nguCanh.TrinhDuyet, 300)
        };
        dbContext.PhienDangNhaps.Add(phien);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToDto(taiKhoan, thiSinh);
        var (accessToken, hetHan) = tokenService.TaoAccessToken(new ThongTinToken(
            taiKhoan.MaTK, taiKhoan.TenDN, taiKhoan.VaiTro, thiSinh?.Msts, taiKhoan.MaBaoMat, taiKhoan.PhaiDoiMatKhau));

        return (new PhienDaCap(accessToken, hetHan, refreshToken, phien.HetHan, ghiNho, dto), phien.MaPhien);
    }

    /// <summary>Cặp tên đăng nhập (chữ thường) + IP dùng làm khóa đếm lần sai (D-AUTH-KHOA-01).</summary>
    private readonly record struct KhoaCap(string TenDN, string DiaChiIP)
    {
        public KhoaCap(string tenDN, NguCanhYeuCau nguCanh)
            : this(Cat(tenDN.Trim().ToLowerInvariant(), 50)!, Cat(nguCanh.DiaChiIP, 45) ?? "khong-ro")
        {
        }
    }

    private Task<DateTime?> LayKhoaDenAsync(KhoaCap khoa, DateTime now, CancellationToken cancellationToken) =>
        dbContext.KhoaDangNhaps.AsNoTracking()
            .Where(x => x.TenDN == khoa.TenDN && x.DiaChiIP == khoa.DiaChiIP && x.KhoaDen > now)
            .Select(x => x.KhoaDen)
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Tăng số lần sai của cặp tên + IP, ghi nhật ký và lưu. Trả thời điểm hết khóa nếu cặp này
    /// vừa bị (hoặc đã bị) tạm khóa; null nếu chưa tới ngưỡng.
    /// </summary>
    private async Task<DateTime?> GhiLanSaiAsync(
        int? mtk,
        KhoaCap khoa,
        string doiTuong,
        string hanhDong,
        string chiTiet,
        NguCanhYeuCau nguCanh,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tenDN = new SqlParameter("@tenDN", SqlDbType.VarChar, 50) { Value = khoa.TenDN };
        var diaChiIP = new SqlParameter("@diaChiIP", SqlDbType.VarChar, 45) { Value = khoa.DiaChiIP };
        var bayGio = new SqlParameter("@bayGio", SqlDbType.DateTime2) { Value = now };
        var hetHieuLuc = new SqlParameter("@hetHieuLuc", SqlDbType.DateTime2)
        {
            Value = now.AddMinutes(-_options.LockoutMinutes)
        };

        // Một câu lệnh nguyên tử (HOLDLOCK): nhiều request sai cùng lúc không làm mất lần đếm.
        // Lần sai cũ hơn thời gian khóa, hoặc khóa đã hết hạn, thì đếm lại từ 1.
        // Cặp đang bị khóa không được cập nhật nên OUTPUT rỗng.
        var soLanSai = await dbContext.Database.SqlQuery<int>($"""
            MERGE dbo.KHOADANGNHAP WITH (HOLDLOCK) AS t
            USING (VALUES ({tenDN}, {diaChiIP})) AS s (TENDN, DIACHIIP)
               ON t.TENDN = s.TENDN AND t.DIACHIIP = s.DIACHIIP
            WHEN MATCHED AND (t.KHOADEN IS NULL OR t.KHOADEN <= {bayGio}) THEN UPDATE SET
                SOLANSAI = CASE WHEN t.KHOADEN IS NOT NULL OR t.LANSAICUOI < {hetHieuLuc}
                                THEN 1 ELSE t.SOLANSAI + 1 END,
                KHOADEN = NULL,
                LANSAICUOI = {bayGio}
            WHEN NOT MATCHED THEN
                INSERT (TENDN, DIACHIIP, SOLANSAI, KHOADEN, LANSAICUOI)
                VALUES (s.TENDN, s.DIACHIIP, 1, NULL, {bayGio})
            OUTPUT inserted.SOLANSAI AS [Value];
            """).ToListAsync(cancellationToken);

        // Dọn các cặp đã lâu không sai để bảng không phình vì tên đăng nhập rác.
        var cu = now.AddDays(-1);
        await dbContext.KhoaDangNhaps
            .Where(x => x.LanSaiCuoi < cu && (x.KhoaDen == null || x.KhoaDen < now))
            .ExecuteDeleteAsync(cancellationToken);

        DateTime? khoaDen;
        if (soLanSai.Count == 0)
        {
            khoaDen = await LayKhoaDenAsync(khoa, now, cancellationToken);
        }
        else if (soLanSai[0] >= _options.MaxFailedLogins)
        {
            khoaDen = now.AddMinutes(_options.LockoutMinutes);
            await dbContext.KhoaDangNhaps
                .Where(x => x.TenDN == khoa.TenDN && x.DiaChiIP == khoa.DiaChiIP)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SoLanSai, 0)
                    .SetProperty(x => x.KhoaDen, khoaDen), cancellationToken);
            GhiNhatKy(mtk, "Tạm khóa đăng nhập", doiTuong,
                $"Sai mật khẩu {_options.MaxFailedLogins} lần liên tiếp từ cùng địa chỉ IP", nguCanh, now);
        }
        else
        {
            khoaDen = null;
        }

        GhiNhatKy(mtk, hanhDong, doiTuong, chiTiet, nguCanh, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return khoaDen;
    }

    private Task XoaLanSaiAsync(KhoaCap khoa, CancellationToken cancellationToken) =>
        dbContext.KhoaDangNhaps
            .Where(x => x.TenDN == khoa.TenDN && x.DiaChiIP == khoa.DiaChiIP)
            .ExecuteDeleteAsync(cancellationToken);

    private Task ThuHoiMoiPhienAsync(int mtk, DateTime now, CancellationToken cancellationToken) =>
        dbContext.PhienDangNhaps
            .Where(x => x.MaTK == mtk && x.ThuHoiLuc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, now), cancellationToken);

    private Task<ThiSinh?> TimThiSinhAsync(TaiKhoan taiKhoan, CancellationToken cancellationToken) =>
        taiKhoan.VaiTro == TaiKhoan.VaiTroThiSinh
            ? dbContext.ThiSinhs.AsNoTracking().SingleOrDefaultAsync(x => x.MaTK == taiKhoan.MaTK, cancellationToken)
            : Task.FromResult<ThiSinh?>(null);

    private void GhiNhatKy(int? mtk, string hanhDong, string doiTuong, string? chiTiet, NguCanhYeuCau nguCanh, DateTime now) =>
        dbContext.NhatKyHoatDongs.Add(new NhatKyHoatDong
        {
            MaTK = mtk,
            Nhom = NhomNhatKy,
            HanhDong = hanhDong,
            DoiTuong = Cat(doiTuong, 200)!,
            ChiTiet = chiTiet,
            ThoiGian = now,
            DiaChiIP = Cat(nguCanh.DiaChiIP, 45)
        });

    private static TaiKhoanHienTaiDto ToDto(TaiKhoan taiKhoan, ThiSinh? thiSinh) => new(
        taiKhoan.MaTK,
        taiKhoan.TenDN,
        thiSinh?.HoTen ?? taiKhoan.HoTen,
        taiKhoan.VaiTro,
        thiSinh?.Msts,
        taiKhoan.PhaiDoiMatKhau);

    private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Cat(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
