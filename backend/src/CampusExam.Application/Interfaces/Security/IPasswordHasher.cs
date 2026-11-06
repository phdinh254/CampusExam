namespace CampusExam.Application.Interfaces.Security;

public enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string hash, string password);
}
