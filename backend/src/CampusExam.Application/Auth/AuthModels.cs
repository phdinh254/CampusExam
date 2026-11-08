using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.Auth;

public sealed class DangNhapRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    public string TenDN { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(128, ErrorMessage = "Mật khẩu không được vượt quá 128 ký tự.")]
    public string MatKhau { get; init; } = string.Empty;

    /// <summary>Giữ đăng nhập trên thiết bị (refresh token sống lâu hơn).</summary>
    public bool GhiNho { get; init; }
}

public sealed class DoiMatKhauRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
    [StringLength(128, ErrorMessage = "Mật khẩu không được vượt quá 128 ký tự.")]
    public string MatKhauCu { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    public string MatKhauMoi { get; init; } = string.Empty;
}

/// <summary>Thông tin tài khoản đang đăng nhập trả cho frontend.</summary>
public sealed record TaiKhoanHienTaiDto(
    int Mtk,
    string TenDN,
    string HoTen,
    string VaiTro,
    string? Msts,
    bool PhaiDoiMatKhau);

/// <summary>Body trả về khi đăng nhập, làm mới phiên, đổi mật khẩu.</summary>
public sealed record PhienDangNhapResponse(
    string AccessToken,
    DateTime HetHan,
    TaiKhoanHienTaiDto TaiKhoan);

/// <summary>Kết quả cấp phiên: access token cho body, refresh token cho cookie.</summary>
public sealed record PhienDaCap(
    string AccessToken,
    DateTime AccessTokenHetHan,
    string RefreshToken,
    DateTime RefreshTokenHetHan,
    bool GhiNho,
    TaiKhoanHienTaiDto TaiKhoan)
{
    public PhienDangNhapResponse ToResponse() => new(AccessToken, AccessTokenHetHan, TaiKhoan);
}

/// <summary>Thông tin request để ghi nhật ký và phiên.</summary>
public sealed record NguCanhYeuCau(string? DiaChiIP, string? TrinhDuyet);

public enum KetQuaDangNhap
{
    ThanhCong,
    SaiThongTin,
    TamKhoa,
    BiVoHieuHoa
}

public sealed record DangNhapResult(KetQuaDangNhap KetQua, PhienDaCap? Phien = null, DateTime? KhoaDen = null);

public enum KetQuaDoiMatKhau
{
    ThanhCong,
    SaiMatKhauCu,
    MatKhauMoiKhongHopLe,
    /// <summary>Sai mật khẩu (đăng nhập hoặc đổi mật khẩu) quá nhiều lần từ cùng IP.</summary>
    TamKhoa
}

public sealed record DoiMatKhauResult(
    KetQuaDoiMatKhau KetQua,
    PhienDaCap? Phien = null,
    IReadOnlyList<string>? Loi = null,
    DateTime? KhoaDen = null);

/// <summary>Thông tin đưa vào access token.</summary>
public sealed record ThongTinToken(
    int Mtk,
    string TenDN,
    string VaiTro,
    string? Msts,
    Guid MaBaoMat,
    bool PhaiDoiMatKhau);
