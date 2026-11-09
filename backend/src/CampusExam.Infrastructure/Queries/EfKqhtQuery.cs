using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Queries;

public sealed class EfKqhtQuery(
    CampusExamDbContext dbContext) : IKqhtQuery
{
    public async Task<IReadOnlyList<KqhtDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await ChieuDto(dbContext.KqHocTaps.AsNoTracking()
                .OrderBy(kqht => kqht.ThuTu)
                .ThenBy(kqht => kqht.MaKQHT))
            .ToListAsync(cancellationToken);
    }

    public async Task<KqhtDto?> GetByIdAsync(
        int maKQHT,
        CancellationToken cancellationToken)
    {
        return await ChieuDto(dbContext.KqHocTaps.AsNoTracking()
                .Where(kqht => kqht.MaKQHT == maKQHT))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<KqhtDto> ChieuDto(IQueryable<KqHocTap> nguon) =>
        nguon.Select(kqht => new KqhtDto(
            kqht.MaKQHT,
            kqht.TenKQHT,
            kqht.ThuTu,
            kqht.MoTa,
            (TrangThaiSuDung)kqht.TrangThai,
            kqht.KyHieu,
            dbContext.CauHois.Count(cauHoi => cauHoi.MaKQHT == kqht.MaKQHT)));
}
