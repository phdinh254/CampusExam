using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.Kqht;

/// <summary>
/// Body của <c>PUT /api/kqht/{maKQHT}</c>. Ký hiệu suy ra từ thứ tự nên không nhận từ client;
/// trạng thái đổi qua <c>PATCH .../trang-thai</c>.
/// </summary>
public sealed class UpdateKqhtCommand
{
    [Required(ErrorMessage = "Tên KQHT là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên KQHT không được vượt quá 100 ký tự.")]
    public string TenKQHT { get; init; } = string.Empty;

    [Range(1, CreateKqhtCommand.MaxThuTu, ErrorMessage = "Thứ tự phải nằm trong khoảng từ 1 đến 26.")]
    public byte ThuTu { get; init; }

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? MoTa { get; init; }
}
