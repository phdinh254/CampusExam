namespace CampusExam.Application.Common;

/// <summary>
/// Kho file nhị phân (D-MEDIA-01). Key do backend sinh, chỉ gồm chữ thường, số, <c>-</c>, <c>.</c>
/// và <c>/</c>; nội dung theo một key không bao giờ bị ghi đè.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    /// <summary>null khi không có file.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Không lỗi khi file không tồn tại.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>Key của file trong <paramref name="prefix"/> sửa lần cuối trước <paramref name="before"/>.</summary>
    IEnumerable<string> ListOlderThan(string prefix, DateTime before);
}
