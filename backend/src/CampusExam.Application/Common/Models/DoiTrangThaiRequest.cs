using System.ComponentModel.DataAnnotations;

namespace CampusExam.Application.Common.Models;

/// <summary>Body của <c>PATCH .../trang-thai</c>: <c>{ "trangThai": "ngung_su_dung" }</c>.</summary>
public sealed class DoiTrangThaiRequest
{
    // Nullable để thiếu trường báo lỗi thay vì ngầm hiểu là giá trị 0 (ngừng sử dụng).
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    [EnumDataType(
        typeof(TrangThaiSuDung),
        ErrorMessage = "Trạng thái chỉ nhận dang_su_dung hoặc ngung_su_dung.")]
    public TrangThaiSuDung? TrangThai { get; init; }
}
