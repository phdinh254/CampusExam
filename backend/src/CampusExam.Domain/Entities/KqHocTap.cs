namespace CampusExam.Domain.Entities;

public class KqHocTap
{
    public int MaKQHT { get; set; }

    public string TenKQHT { get; set; } = string.Empty;

    public byte ThuTu { get; set; }

    public string? MoTa { get; set; }

    public short TrangThai { get; set; }

    public string KyHieu { get; set; } = string.Empty;
}
