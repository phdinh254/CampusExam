namespace CampusExam.Domain.Entities;

public class TaiNguyenCauHoi
{
    public int MaTN { get; set; }

    public int MaCH { get; set; }

    public string ViTri { get; set; } = string.Empty;

    public short ThuTu { get; set; }

    public string TenFile { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public DateTime NgayTao { get; set; }
}
