namespace CampusExam.Application.Kqht;

public interface IKqhtQuery
{
    Task<IReadOnlyList<KqhtDto>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<KqhtDto?> GetByIdAsync(
        int maKQHT,
        CancellationToken cancellationToken);
}
