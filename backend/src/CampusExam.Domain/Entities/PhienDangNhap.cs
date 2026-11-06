namespace CampusExam.Domain.Entities;

public class PhienDangNhap
{
    public long MaPhien { get; set; }

    public int MaTK { get; set; }

    /// <summary>SHA-256 của refresh token; không lưu token thô.</summary>
    public byte[] TokenHash { get; set; } = [];

    public DateTime NgayTao { get; set; }

    public DateTime HetHan { get; set; }

    public DateTime? ThuHoiLuc { get; set; }

    /// <summary>Phiên mới được tạo khi xoay vòng token.</summary>
    public long? MaPhienThay { get; set; }

    public string? DiaChiIP { get; set; }

    public string? TrinhDuyet { get; set; }
}
