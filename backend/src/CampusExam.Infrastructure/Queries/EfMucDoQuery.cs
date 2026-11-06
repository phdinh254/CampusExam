using CampusExam.Application.MucDo;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Queries;

public sealed class EfMucDoQuery(
    CampusExamDbContext dbContext) : IMucDoQuery
{
    public async Task<IReadOnlyList<MucDoDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.MucDos
            .AsNoTracking()
            .OrderBy(mucDo => mucDo.MaMD)
            .Select(mucDo => new MucDoDto(
                mucDo.MaMD,
                mucDo.TenMD))
            .ToListAsync(cancellationToken);
    }
}
