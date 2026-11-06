namespace CampusExam.Application.Accounts;

public enum CreateInitialAdminStatus
{
    Created,
    AdminAlreadyExists,
    DuplicateTenDN
}

public sealed record CreateInitialAdminResult(CreateInitialAdminStatus Status, int? MaTK)
{
    public static CreateInitialAdminResult Success(int maTK) => new(CreateInitialAdminStatus.Created, maTK);

    public static CreateInitialAdminResult Failure(CreateInitialAdminStatus status) => new(status, null);
}
