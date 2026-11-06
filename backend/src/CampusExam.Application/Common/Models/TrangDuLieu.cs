using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.Common.Models;

/// <summary>Một trang kết quả của API danh sách có phân trang.</summary>
public sealed record TrangDuLieu<T>(
    IReadOnlyList<T> DuLieu,
    int Trang,
    int KichThuocTrang,
    int TongSoDong)
{
    public int TongSoTrang => KichThuocTrang <= 0
        ? 0
        : (TongSoDong + KichThuocTrang - 1) / KichThuocTrang;
}

/// <summary>Tham số phân trang chung, bind từ query <c>?trang=&amp;kichThuocTrang=</c>.</summary>
public class TruyVanTrang
{
    public const int KichThuocMacDinh = 20;
    public const int KichThuocToiDa = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải từ 1 trở lên.")]
    public int Trang { get; init; } = 1;

    [Range(1, KichThuocToiDa, ErrorMessage = "Kích thước trang phải từ 1 đến 100.")]
    public int KichThuocTrang { get; init; } = KichThuocMacDinh;

    public int SoDongBoQua => (Trang - 1) * KichThuocTrang;
}
