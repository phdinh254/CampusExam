using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.CauHoi;

public interface ICauHoiQuery
{
    Task<TrangDuLieu<CauHoiTomTatDto>> GetPageAsync(
        TruyVanCauHoi truyVan,
        CancellationToken cancellationToken);

    Task<ThongKeCauHoiDto> GetThongKeAsync(CancellationToken cancellationToken);

    Task<CauHoiChiTietDto?> GetByIdAsync(int maCH, CancellationToken cancellationToken);
}

public interface ICauHoiCommandService
{
    /// <summary>Thêm câu hỏi; KQHT phải tồn tại và đang sử dụng.</summary>
    Task<KetQua<CauHoiChiTietDto>> TaoAsync(
        TaoCauHoiCommand command,
        CancellationToken cancellationToken);

    /// <summary>Sửa nội dung; lệch phiên bản trả <see cref="ErrorCodes.ConcurrencyConflict"/>.</summary>
    Task<KetQua<CauHoiChiTietDto>> SuaAsync(
        int maCH,
        SuaCauHoiCommand command,
        CancellationToken cancellationToken);

    /// <summary>Chuyển tự do hai chiều; đề đã sinh dùng bản sao nên không bị ảnh hưởng.</summary>
    Task<KetQua<CauHoiChiTietDto>> DoiTrangThaiAsync(
        int maCH,
        TrangThaiSuDung trangThai,
        CancellationToken cancellationToken);

    /// <summary>Xóa cứng; chỉ khi câu hỏi chưa nằm trong đề nào.</summary>
    Task<KetQua<KhongCo>> XoaAsync(int maCH, CancellationToken cancellationToken);
}
