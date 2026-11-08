namespace CampusExam.Infrastructure.Imports;

/// <summary>
/// Giới hạn chống zip bomb khi đọc DOCX: tệp tải lên chỉ 20 MB nhưng nội dung nén có thể
/// phình ra rất lớn khi giải nén.
/// </summary>
/// <param name="TongDungLuongGiaiNen">Tổng dung lượng (byte) mọi thành phần sau giải nén.</param>
/// <param name="SoMucToiDa">Số thành phần tối đa trong gói zip.</param>
/// <param name="KyTuMoiPhan">Số ký tự tối đa của một phần XML (OpenSettings.MaxCharactersInPart).</param>
public sealed record GioiHanTepDocx(long TongDungLuongGiaiNen, int SoMucToiDa, long KyTuMoiPhan)
{
    /// <summary>Đủ rộng cho đề IU03 (41 câu, 74 ảnh) và các đề lớn hơn nhiều lần.</summary>
    public static GioiHanTepDocx MacDinh { get; } = new(
        TongDungLuongGiaiNen: 200L * 1024 * 1024,
        SoMucToiDa: 5_000,
        KyTuMoiPhan: 50_000_000);
}
