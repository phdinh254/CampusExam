using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.CauHoi;

/// <summary>Phần nội dung chung của thêm và sửa câu hỏi.</summary>
public abstract class NoiDungCauHoiCommand
{
    [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc.")]
    [StringLength(4000, ErrorMessage = "Nội dung câu hỏi không được vượt quá 4000 ký tự.")]
    public string NoiDung { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phương án A là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Phương án không được vượt quá 1000 ký tự.")]
    public string PhuongAnA { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phương án B là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Phương án không được vượt quá 1000 ký tự.")]
    public string PhuongAnB { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phương án C là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Phương án không được vượt quá 1000 ký tự.")]
    public string PhuongAnC { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phương án D là bắt buộc.")]
    [StringLength(1000, ErrorMessage = "Phương án không được vượt quá 1000 ký tự.")]
    public string PhuongAnD { get; init; } = string.Empty;

    [Required(ErrorMessage = "Đáp án đúng là bắt buộc.")]
    [RegularExpression("^[A-Da-d]$", ErrorMessage = "Đáp án đúng chỉ nhận A, B, C hoặc D.")]
    public string DapAnDung { get; init; } = string.Empty;

    /// <summary>true: giữ nguyên thứ tự phương án khi sinh đề (ví dụ "Cả A và B đều đúng").</summary>
    public bool CoDinhDapAn { get; init; }
}

/// <summary>
/// Body của <c>POST /api/cau-hoi</c>. Không nhận mã câu hỏi/STT: backend cấp STT kế tiếp theo
/// (KQHT, mức độ). Câu hỏi mới luôn ở trạng thái đang sử dụng.
/// </summary>
public sealed class TaoCauHoiCommand : NoiDungCauHoiCommand
{
    [Range(1, int.MaxValue, ErrorMessage = "KQHT là bắt buộc.")]
    public int MaKQHT { get; init; }

    [Range(1, 3, ErrorMessage = "Mức độ chỉ nhận 1, 2 hoặc 3.")]
    public byte MaMD { get; init; }
}

/// <summary>
/// Body của <c>PUT /api/cau-hoi/{maCH}</c>. Không đổi được KQHT và mức độ vì chúng quyết định
/// mã câu hỏi; muốn phân loại lại thì tạo câu mới và ngừng dùng câu cũ.
/// </summary>
public sealed class SuaCauHoiCommand : NoiDungCauHoiCommand, IValidatableObject
{
    [Required(ErrorMessage = "Phiên bản là bắt buộc.")]
    public string PhienBan { get; init; } = string.Empty;

    public byte[]? PhienBanBytes() => DocPhienBan(PhienBan);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(PhienBan) && PhienBanBytes() is null)
        {
            yield return new ValidationResult("Phiên bản không hợp lệ.", [nameof(PhienBan)]);
        }
    }

    /// <summary>rowversion của SQL Server luôn 8 byte.</summary>
    private static byte[]? DocPhienBan(string giaTri)
    {
        var buffer = new byte[8];
        return Convert.TryFromBase64String(giaTri, buffer, out var soByte) && soByte == 8 ? buffer : null;
    }
}
