namespace CampusExam.Application.Common;

public enum LoaiLoi
{
    /// <summary>HTTP 400: dữ liệu sai nhưng chỉ phát hiện được khi đối chiếu database.</summary>
    KhongHopLe,

    /// <summary>HTTP 404.</summary>
    KhongTimThay,

    /// <summary>HTTP 409: trùng, sai trạng thái hoặc ghi đè.</summary>
    XungDot,

    /// <summary>HTTP 410: tài nguyên tạm (bản xem trước) đã hết hạn.</summary>
    HetHan
}

/// <summary>Lỗi nghiệp vụ trả từ tầng Application; Api đổi thành ProblemDetails.</summary>
/// <param name="Truong">Tên trường JSON bị lỗi, chỉ dùng với <see cref="LoaiLoi.KhongHopLe"/>.</param>
public sealed record LoiNghiepVu(
    LoaiLoi Loai,
    string Code,
    string TieuDe,
    string? ChiTiet = null,
    string? Truong = null);

/// <summary>Kết quả lệnh: có giá trị khi thành công, có <see cref="Loi"/> khi thất bại.</summary>
public sealed class KetQua<T>
{
    private KetQua(T? giaTri, LoiNghiepVu? loi)
    {
        GiaTri = giaTri;
        Loi = loi;
    }

    public T? GiaTri { get; }

    public LoiNghiepVu? Loi { get; }

    public bool ThanhCong => Loi is null;

    public static KetQua<T> Ok(T giaTri) => new(giaTri, null);

    public static implicit operator KetQua<T>(LoiNghiepVu loi) => new(default, loi);
}

/// <summary>Giá trị cho lệnh không trả dữ liệu (xóa).</summary>
public readonly record struct KhongCo;
