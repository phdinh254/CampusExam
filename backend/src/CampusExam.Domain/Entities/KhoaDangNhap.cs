namespace CampusExam.Domain.Entities;

/// <summary>
/// Số lần nhập sai mật khẩu theo cặp tên đăng nhập + IP (D-AUTH-KHOA-01). Khóa theo tên đăng nhập
/// chứ không theo tài khoản, để tên không tồn tại cũng bị khóa như tên có thật.
/// </summary>
public class KhoaDangNhap
{
    /// <summary>Chữ thường, đã cắt khoảng trắng.</summary>
    public string TenDN { get; set; } = string.Empty;

    public string DiaChiIP { get; set; } = string.Empty;

    public int SoLanSai { get; set; }

    public DateTime? KhoaDen { get; set; }

    public DateTime LanSaiCuoi { get; set; }
}
