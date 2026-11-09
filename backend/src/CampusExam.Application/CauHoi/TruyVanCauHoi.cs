using System.ComponentModel.DataAnnotations;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.CauHoi;

/// <summary>Query của <c>GET /api/cau-hoi</c>.</summary>
public sealed class TruyVanCauHoi : TruyVanTrang
{
    public const string SapXepMacDinh = "-ngayCapNhat";

    /// <summary>Tìm trong mã câu hỏi và nội dung.</summary>
    [StringLength(200, ErrorMessage = "Từ khóa không được vượt quá 200 ký tự.")]
    public string? TuKhoa { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Mã KQHT không hợp lệ.")]
    public int? MaKQHT { get; init; }

    [Range(1, 3, ErrorMessage = "Mức độ chỉ nhận 1, 2 hoặc 3.")]
    public byte? MaMD { get; init; }

    // Chuỗi thay vì enum: binder query không đọc tên JsonStringEnumMemberName.
    [RegularExpression(
        "^(dang_su_dung|ngung_su_dung)$",
        ErrorMessage = "Trạng thái chỉ nhận dang_su_dung hoặc ngung_su_dung.")]
    public string? TrangThai { get; init; }

    /// <summary>Một trường trong allowlist, tiền tố <c>-</c> là giảm dần.</summary>
    [RegularExpression(
        "^-?(maCauHoi|ngayCapNhat|ngayTao)$",
        ErrorMessage = "Chỉ sắp xếp theo maCauHoi, ngayCapNhat hoặc ngayTao.")]
    public string? SapXep { get; init; }

    public TrangThaiSuDung? TrangThaiEnum => TrangThai switch
    {
        "dang_su_dung" => TrangThaiSuDung.DangSuDung,
        "ngung_su_dung" => TrangThaiSuDung.NgungSuDung,
        _ => null
    };
}
