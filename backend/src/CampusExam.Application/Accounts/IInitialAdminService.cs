namespace CampusExam.Application.Accounts;

/// <summary>Tạo tài khoản quản trị đầu tiên; từ chối khi hệ thống đã có quản trị viên.</summary>
public interface IInitialAdminService
{
    Task<CreateInitialAdminResult> CreateAsync(
        CreateInitialAdminCommand command,
        CancellationToken cancellationToken);
}
