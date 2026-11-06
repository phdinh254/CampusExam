using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.Accounts;

public sealed class CreateInitialAdminCommand : IValidatableObject
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [RegularExpression(
        "^[A-Za-z0-9._-]{3,50}$",
        ErrorMessage = "Tên đăng nhập dài 3-50 ký tự, chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới, gạch ngang.")]
    public string TenDN { get; init; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string HoTen { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu dài 8-128 ký tự.")]
    public string MatKhau { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(HoTen))
        {
            yield return new ValidationResult("Họ tên là bắt buộc.", [nameof(HoTen)]);
        }

        // Độ dài và bắt buộc đã do attribute kiểm tra; ở đây chỉ thêm các quy tắc nội dung.
        if (string.IsNullOrEmpty(MatKhau) || MatKhau.Length < ChinhSachMatKhau.DoDaiToiThieu)
        {
            yield break;
        }

        foreach (var loi in ChinhSachMatKhau.KiemTra(MatKhau, TenDN)
                     .Where(loi => !loi.StartsWith("Mật khẩu dài", StringComparison.Ordinal)))
        {
            yield return new ValidationResult(loi, [nameof(MatKhau)]);
        }
    }
}
