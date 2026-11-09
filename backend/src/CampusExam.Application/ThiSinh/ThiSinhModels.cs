using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CampusExam.Application.Common.Json;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.ThiSinh;

/// <summary>THISINH.GIOITINH: 0 nữ, 1 nam; NULL là không khai báo.</summary>
[JsonConverter(typeof(EnumChuoiConverter<GioiTinh>))]
public enum GioiTinh : short
{
    [JsonStringEnumMemberName("nu")]
    Nu = 0,

    [JsonStringEnumMemberName("nam")]
    Nam = 1
}

/// <summary>TAIKHOAN.TRANGTHAI: 0 khóa, 1 hoạt động.</summary>
[JsonConverter(typeof(EnumChuoiConverter<TrangThaiTaiKhoan>))]
public enum TrangThaiTaiKhoan : short
{
    [JsonStringEnumMemberName("khoa")]
    Khoa = 0,

    [JsonStringEnumMemberName("hoat_dong")]
    HoatDong = 1
}

/// <param name="DaDoiMatKhau">false khi thí sinh còn dùng mật khẩu mặc định (PHAIDOIMATKHAU = 1).</param>
/// <param name="SoLuotThi">Số bài làm đã bắt đầu.</param>
public sealed record ThiSinhTomTatDto(
    string Msts,
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh? GioiTinh,
    string? Lop,
    TrangThaiTaiKhoan TrangThaiTaiKhoan,
    bool DaDoiMatKhau,
    int SoLuotThi,
    DateTime NgayTao);

/// <param name="SoLanPhanCong">Số kỳ thi được phân công; lớn hơn 0 thì không xóa được.</param>
public sealed record ThiSinhChiTietDto(
    string Msts,
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh? GioiTinh,
    string? Lop,
    string? NoiSinh,
    string? Cccd,
    TrangThaiTaiKhoan TrangThaiTaiKhoan,
    bool DaDoiMatKhau,
    DateTime? LanDNCuoi,
    int SoLuotThi,
    int SoLanPhanCong,
    DateTime NgayTao);

/// <summary>Query của <c>GET /api/thi-sinh</c>.</summary>
public sealed class TruyVanThiSinh : TruyVanTrang
{
    public const string SapXepMacDinh = "msts";

    /// <summary>Tìm trong mã số và họ tên.</summary>
    [StringLength(100, ErrorMessage = "Từ khóa không được vượt quá 100 ký tự.")]
    public string? TuKhoa { get; init; }

    [StringLength(50, ErrorMessage = "Lớp không được vượt quá 50 ký tự.")]
    public string? Lop { get; init; }

    [RegularExpression("^(khoa|hoat_dong)$", ErrorMessage = "Trạng thái chỉ nhận khoa hoặc hoat_dong.")]
    public string? TrangThai { get; init; }

    /// <summary>false: chỉ thí sinh còn dùng mật khẩu mặc định.</summary>
    public bool? DaDoiMatKhau { get; init; }

    [RegularExpression(
        "^-?(msts|hoTen|lop|ngayTao)$",
        ErrorMessage = "Chỉ sắp xếp theo msts, hoTen, lop hoặc ngayTao.")]
    public string? SapXep { get; init; }

    public TrangThaiTaiKhoan? TrangThaiEnum => TrangThai switch
    {
        "khoa" => TrangThaiTaiKhoan.Khoa,
        "hoat_dong" => TrangThaiTaiKhoan.HoatDong,
        _ => null
    };
}

/// <summary>Hồ sơ chung của thêm và sửa thí sinh.</summary>
public abstract class HoSoThiSinhCommand : IValidatableObject
{
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string HoTen { get; init; } = string.Empty;

    // Nullable để thiếu trường báo lỗi thay vì ngầm hiểu là 01/01/0001.
    [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
    public DateOnly? NgaySinh { get; init; }

    [EnumDataType(typeof(GioiTinh), ErrorMessage = "Giới tính chỉ nhận nu hoặc nam.")]
    public GioiTinh? GioiTinh { get; init; }

    [StringLength(50, ErrorMessage = "Lớp không được vượt quá 50 ký tự.")]
    public string? Lop { get; init; }

    [StringLength(200, ErrorMessage = "Nơi sinh không được vượt quá 200 ký tự.")]
    public string? NoiSinh { get; init; }

    [RegularExpression("^[0-9]{12}$", ErrorMessage = "CCCD gồm đúng 12 chữ số.")]
    public string? Cccd { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ngày sinh là mật khẩu mặc định nên phải là ngày thật.
        if (NgaySinh is { } ngay && (ngay.Year < 1900 || ngay > DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            yield return new ValidationResult("Ngày sinh không hợp lệ.", [nameof(NgaySinh)]);
        }
    }
}

/// <summary>
/// Body của <c>POST /api/thi-sinh</c>. Tạo kèm tài khoản: tên đăng nhập = MSTS, mật khẩu
/// mặc định = ngày sinh <c>ddMMyyyy</c>, bắt buộc đổi ở lần đăng nhập đầu (D-THISINH-01).
/// </summary>
public sealed class TaoThiSinhCommand : HoSoThiSinhCommand
{
    [Required(ErrorMessage = "Mã số thí sinh là bắt buộc.")]
    [StringLength(20, ErrorMessage = "Mã số thí sinh không được vượt quá 20 ký tự.")]
    [RegularExpression(
        "^[A-Za-z0-9._-]+$",
        ErrorMessage = "Mã số thí sinh chỉ gồm chữ không dấu, số và các ký tự . _ -")]
    public string Msts { get; init; } = string.Empty;
}

/// <summary>Body của <c>PUT /api/thi-sinh/{msts}</c>. MSTS không đổi được (là tên đăng nhập).</summary>
public sealed class SuaThiSinhCommand : HoSoThiSinhCommand;

public sealed class DoiTrangThaiTaiKhoanRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
    [EnumDataType(typeof(TrangThaiTaiKhoan), ErrorMessage = "Trạng thái chỉ nhận khoa hoặc hoat_dong.")]
    public TrangThaiTaiKhoan? TrangThai { get; init; }
}

/// <summary>Quản trị viên thực hiện thao tác, để ghi NHATKYHD.</summary>
public sealed record NguoiThucHien(int MaTK, string? DiaChiIP);
