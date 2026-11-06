using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Queries;

public sealed class EfKqhtQuery(
    CampusExamDbContext dbContext) : IKqhtQuery
{
    public async Task<IReadOnlyList<KqhtDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.KqHocTaps
            .AsNoTracking()
            .OrderBy(kqht => kqht.ThuTu)
            .ThenBy(kqht => kqht.MaKQHT)
            .Select(kqht => new KqhtDto(
                kqht.MaKQHT,
                kqht.TenKQHT,
                kqht.ThuTu,
                kqht.MoTa,
                (TrangThaiSuDung)kqht.TrangThai,
                kqht.KyHieu))
            .ToListAsync(cancellationToken);
    }

    public async Task<KqhtDto?> GetByIdAsync(
        int maKQHT,
        CancellationToken cancellationToken)
    {
        return await dbContext.KqHocTaps
            .AsNoTracking()
            .Where(kqht => kqht.MaKQHT == maKQHT)
            .Select(kqht => new KqhtDto(
                kqht.MaKQHT,
                kqht.TenKQHT,
                kqht.ThuTu,
                kqht.MoTa,
                (TrangThaiSuDung)kqht.TrangThai,
                kqht.KyHieu))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
