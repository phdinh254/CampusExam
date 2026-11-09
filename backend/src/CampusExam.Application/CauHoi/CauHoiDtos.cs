using CampusExam.Application.Common.Models;

namespace CampusExam.Application.CauHoi;

public sealed record KqhtTomTatDto(int MaKQHT, byte ThuTu, string KyHieu);

/// <summary>Một dòng của bảng câu hỏi; không có phương án và đáp án để danh sách nhẹ.</summary>
/// <param name="NgayCapNhat">Ngày cập nhật, hoặc ngày tạo nếu chưa sửa lần nào.</param>
public sealed record CauHoiTomTatDto(
    int MaCH,
    string MaCauHoi,
    string TomTat,
    KqhtTomTatDto Kqht,
    byte MaMD,
    TrangThaiSuDung TrangThai,
    bool CoHinhAnh,
    DateTime NgayCapNhat);

public sealed record HinhAnhCauHoiDto(
    int MaTN,
    string ViTri,
    short ThuTu,
    string Url,
    string? AltText);

/// <summary>Chi tiết câu hỏi cho quản trị viên; có đáp án đúng nên không được trả cho thí sinh.</summary>
/// <param name="SoDeDaDung">Số đề gốc chứa câu hỏi. Lớn hơn 0 thì không xóa cứng được.</param>
/// <param name="PhienBan">rowversion dạng base64; gửi lại khi sửa để chống ghi đè.</param>
public sealed record CauHoiChiTietDto(
    int MaCH,
    string MaCauHoi,
    int MaKQHT,
    byte MaMD,
    string NoiDung,
    string PhuongAnA,
    string PhuongAnB,
    string PhuongAnC,
    string PhuongAnD,
    string DapAnDung,
    bool CoDinhDapAn,
    TrangThaiSuDung TrangThai,
    IReadOnlyList<HinhAnhCauHoiDto> HinhAnh,
    int SoDeDaDung,
    string PhienBan,
    DateTime NgayTao,
    DateTime? NgayCapNhat);

/// <param name="SoKQHT">Số KQHT đang sử dụng.</param>
public sealed record ThongKeCauHoiDto(
    int TongSo,
    int DangSuDung,
    int NgungSuDung,
    int SoKQHT);
