namespace CampusExam.Domain.Entities;

public class NhatKyHoatDong
{
    public int MaNhatKy { get; set; }

    /// <summary>NULL khi thao tác do hệ thống thực hiện.</summary>
    public int? MaTK { get; set; }

    public string Nhom { get; set; } = string.Empty;

    public string HanhDong { get; set; } = string.Empty;

    public string DoiTuong { get; set; } = string.Empty;

    public string? ChiTiet { get; set; }

    public DateTime ThoiGian { get; set; }

    public string? DiaChiIP { get; set; }
}
