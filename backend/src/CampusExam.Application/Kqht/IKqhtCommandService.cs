namespace CampusExam.Application.Kqht;

public interface IKqhtCommandService
{
    Task<CreateKqhtResult> CreateAsync(
        CreateKqhtCommand command,
        CancellationToken cancellationToken);
}
