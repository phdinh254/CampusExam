using System.Globalization;

namespace CampusExam.Application.Accounts;

/// <summary>Chính sách mật khẩu chung cho mọi tài khoản (D-THISINH-01).</summary>
public static class ChinhSachMatKhau
{
    public const int DoDaiToiThieu = 8;
    public const int DoDaiToiDa = 128;
    public const string DinhDangNgaySinh = "ddMMyyyy";

    /// <summary>Mật khẩu mặc định của thí sinh: ngày sinh dạng ddMMyyyy, ví dụ 14/03/2005 → 14032005.</summary>
    public static string MatKhauMacDinh(DateOnly ngaySinh) =>
        ngaySinh.ToString(DinhDangNgaySinh, CultureInfo.InvariantCulture);

    /// <summary>Trả danh sách lỗi; rỗng nghĩa là mật khẩu hợp lệ.</summary>
    public static IReadOnlyList<string> KiemTra(string? matKhau, string? tenDN, DateOnly? ngaySinh = null)
    {
        var loi = new List<string>();
        if (string.IsNullOrEmpty(matKhau))
        {
            loi.Add("Mật khẩu là bắt buộc.");
            return loi;
        }

        if (matKhau.Length is < DoDaiToiThieu or > DoDaiToiDa)
        {
            loi.Add($"Mật khẩu dài {DoDaiToiThieu}-{DoDaiToiDa} ký tự.");
        }

        if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
        {
            loi.Add("Mật khẩu phải có cả chữ và số.");
        }

        if (!string.IsNullOrWhiteSpace(tenDN)
            && matKhau.Contains(tenDN.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            loi.Add("Mật khẩu không được chứa tên đăng nhập.");
        }

        if (ngaySinh is { } ngay && matKhau.Contains(MatKhauMacDinh(ngay), StringComparison.Ordinal))
        {
            loi.Add("Mật khẩu không được chứa ngày sinh.");
        }

        return loi;
    }
}
