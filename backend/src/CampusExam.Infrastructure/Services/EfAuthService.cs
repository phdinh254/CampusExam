using System.Security.Cryptography;
using System.Text;
using CampusExam.Application.Accounts;
using CampusExam.Application.Auth;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
    TimeProvider timeProvider) : IAuthService
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
        var taiKhoan = await dbContext.TaiKhoans.SingleOrDefaultAsync(x => x.TenDN == tenDN, cancellationToken);

        if (taiKhoan is null)
        {
            s_hashGia ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            passwordHasher.Verify(s_hashGia, request.MatKhau);
            GhiNhatKy(null, "Đăng nhập thất bại", tenDN, "Tên đăng nhập không tồn tại", nguCanh, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DangNhapResult(KetQuaDangNhap.SaiThongTin);
        }

        if (taiKhoan.KhoaDen > now)
        {
            return new DangNhapResult(KetQuaDangNhap.TamKhoa, KhoaDen: taiKhoan.KhoaDen);
        }

        var kiemTra = passwordHasher.Verify(taiKhoan.MatKhauHash, request.MatKhau);
        if (kiemTra == PasswordCheck.Failed)
        {
            taiKhoan.SoLanDNSai++;
            if (taiKhoan.SoLanDNSai >= _options.MaxFailedLogins)
            {
                taiKhoan.SoLanDNSai = 0;
                taiKhoan.KhoaDen = now.AddMinutes(_options.LockoutMinutes);
                GhiNhatKy(taiKhoan.MaTK, "Tạm khóa đăng nhập", taiKhoan.TenDN,
                    $"Sai mật khẩu {_options.MaxFailedLogins} lần liên tiếp", nguCanh, now);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new DangNhapResult(KetQuaDangNhap.TamKhoa, KhoaDen: taiKhoan.KhoaDen);
            }

            GhiNhatKy(taiKhoan.MaTK, "Đăng nhập thất bại", taiKhoan.TenDN, "Sai mật khẩu", nguCanh, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DangNhapResult(KetQuaDangNhap.SaiThongTin);
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

        taiKhoan.SoLanDNSai = 0;
        taiKhoan.KhoaDen = null;
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

        if (passwordHasher.Verify(taiKhoan.MatKhauHash, request.MatKhauCu) == PasswordCheck.Failed)
        {
            GhiNhatKy(mtk, "Đổi mật khẩu thất bại", taiKhoan.TenDN, "Sai mật khẩu hiện tại", nguCanh, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DoiMatKhauResult(KetQuaDoiMatKhau.SaiMatKhauCu);
        }

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

        return new DoiMatKhauResult(KetQuaDoiMatKhau.ThanhCong, phien.Phien);
    }

    public Task<bool> AccessTokenConHieuLucAsync(int mtk, Guid maBaoMat, CancellationToken cancellationToken) =>
        dbContext.TaiKhoans.AsNoTracking().AnyAsync(
            x => x.MaTK == mtk && x.MaBaoMat == maBaoMat && x.TrangThai == TaiKhoan.TrangThaiHoatDong,
            cancellationToken);

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
