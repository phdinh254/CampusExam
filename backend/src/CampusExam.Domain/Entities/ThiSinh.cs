namespace CampusExam.Domain.Entities;

public class ThiSinh
{
    public const short GioiTinhNu = 0;
    public const short GioiTinhNam = 1;

    /// <summary>Mã số thí sinh, đồng thời là tên đăng nhập (D-THISINH-01).</summary>
    public string Msts { get; set; } = string.Empty;

    public int MaTK { get; set; }

    /// <summary>NULL khi thêm thủ công.</summary>
    public int? MaLanImport { get; set; }

    public string HoTen { get; set; } = string.Empty;

    public DateOnly NgaySinh { get; set; }

    public short? GioiTinh { get; set; }

    public string? Lop { get; set; }

    public string? NoiSinh { get; set; }

    public string? Cccd { get; set; }

    public DateTime NgayTao { get; set; }
}
