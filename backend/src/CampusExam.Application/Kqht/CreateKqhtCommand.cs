using System.ComponentModel.DataAnnotations;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.Kqht;

public sealed class CreateKqhtCommand : IValidatableObject
{
    /// <summary>Số KQHT tối đa: ký hiệu là chữ cái A-Z theo thứ tự.</summary>
    public const byte MaxThuTu = 26;

    [Required(ErrorMessage = "Tên KQHT là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên KQHT không được vượt quá 100 ký tự.")]
    public string TenKQHT { get; init; } = string.Empty;

    [Range(1, MaxThuTu, ErrorMessage = "Thứ tự phải nằm trong khoảng từ 1 đến 26.")]
    public byte ThuTu { get; init; }

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? MoTa { get; init; }

    [EnumDataType(
        typeof(TrangThaiSuDung),
        ErrorMessage = "Trạng thái chỉ nhận dang_su_dung hoặc ngung_su_dung.")]
    public TrangThaiSuDung TrangThai { get; init; } = TrangThaiSuDung.DangSuDung;

    [Required(ErrorMessage = "Ký hiệu là bắt buộc.")]
    [RegularExpression("^[A-Za-z]$", ErrorMessage = "Ký hiệu phải là một chữ cái từ A đến Z.")]
    public string KyHieu { get; init; } = string.Empty;

    /// <summary>Ký hiệu đúng của KQHT thứ <paramref name="thuTu"/>: 1 → A, 2 → B, ...</summary>
    public static char KyHieuTheoThuTu(byte thuTu) => (char)('A' + thuTu - 1);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(KyHieu) || ThuTu is < 1 or > MaxThuTu)
        {
            yield break;
        }

        var expected = KyHieuTheoThuTu(ThuTu);
        if (char.ToUpperInvariant(KyHieu[0]) != expected)
        {
            yield return new ValidationResult(
                $"KQHT thứ {ThuTu} phải có ký hiệu {expected} (A - KQHT 1, B - KQHT 2, ...).",
                [nameof(KyHieu), nameof(ThuTu)]);
        }
    }
}
