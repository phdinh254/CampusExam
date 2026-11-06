namespace CampusExam.Domain.Entities;

public class TaiKhoan
{
    public const string VaiTroAdmin = "ADMIN";
    public const string VaiTroThiSinh = "THISINH";

    public const short TrangThaiKhoa = 0;
    public const short TrangThaiHoatDong = 1;

    public int MaTK { get; set; }

    public string TenDN { get; set; } = string.Empty;

    public string MatKhauHash { get; set; } = string.Empty;

    public string VaiTro { get; set; } = string.Empty;

    public string HoTen { get; set; } = string.Empty;

    public short TrangThai { get; set; }

    public DateTime? LanDNCuoi { get; set; }

    public DateTime NgayTao { get; set; }

    public bool PhaiDoiMatKhau { get; set; }

    public int SoLanDNSai { get; set; }

    public DateTime? KhoaDen { get; set; }

    /// <summary>Đổi giá trị khi đổi mật khẩu hoặc khóa tài khoản để vô hiệu các phiên cũ.</summary>
    public Guid MaBaoMat { get; set; }
}
