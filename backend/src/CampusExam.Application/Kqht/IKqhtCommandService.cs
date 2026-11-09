using CampusExam.Application.Common;
using CampusExam.Application.Common.Models;

namespace CampusExam.Application.Kqht;

public interface IKqhtCommandService
{
    Task<CreateKqhtResult> CreateAsync(
        CreateKqhtCommand command,
        CancellationToken cancellationToken);

    /// <summary>Sửa tên, mô tả, thứ tự. Không đổi được thứ tự khi KQHT đã có câu hỏi.</summary>
    Task<KetQua<KqhtDto>> UpdateAsync(
        int maKQHT,
        UpdateKqhtCommand command,
        CancellationToken cancellationToken);

    Task<KetQua<KqhtDto>> DoiTrangThaiAsync(
        int maKQHT,
        TrangThaiSuDung trangThai,
        CancellationToken cancellationToken);

    /// <summary>Xóa cứng; chỉ khi chưa có câu hỏi hoặc ma trận nào tham chiếu (D-KQHT-01).</summary>
    Task<KetQua<KhongCo>> XoaAsync(
        int maKQHT,
        CancellationToken cancellationToken);
}
