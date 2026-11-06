namespace CampusExam.Application.MucDo;

public interface IMucDoQuery
{
    Task<IReadOnlyList<MucDoDto>> GetAllAsync(
        CancellationToken cancellationToken);
}
