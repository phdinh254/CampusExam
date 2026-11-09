namespace CampusExam.Domain.Entities;

public class LanImport
{
    public const string LoaiCauHoi = "CAUHOI";
    public const string LoaiThiSinh = "THISINH";

    public int MaLanImport { get; set; }

    public int MaTK { get; set; }

    public string LoaiImport { get; set; } = string.Empty;

    public string TenFile { get; set; } = string.Empty;

    public int TongDong { get; set; }

    public int SoThanhCong { get; set; }

    public int SoDongLoi { get; set; }

    public DateTime ThoiGian { get; set; }

    public byte TrangThai { get; set; }
}
