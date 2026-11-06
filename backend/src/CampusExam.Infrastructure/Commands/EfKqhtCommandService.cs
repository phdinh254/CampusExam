using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Domain.Entities;
using CampusExam.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusExam.Infrastructure.Commands;

public sealed class EfKqhtCommandService(
    CampusExamDbContext dbContext) : IKqhtCommandService
{
    public async Task<CreateKqhtResult> CreateAsync(
        CreateKqhtCommand command,
        CancellationToken cancellationToken)
    {
        var kyHieu = command.KyHieu.ToUpperInvariant();

        var daTonTai = await dbContext.KqHocTaps
            .AsNoTracking()
            .AnyAsync(kqht => kqht.KyHieu == kyHieu, cancellationToken);

        if (daTonTai)
        {
            return CreateKqhtResult.Duplicate();
        }

        var kqht = new KqHocTap
        {
            TenKQHT = command.TenKQHT.Trim(),
            ThuTu = command.ThuTu,
            MoTa = string.IsNullOrWhiteSpace(command.MoTa)
                ? null
                : command.MoTa.Trim(),
            TrangThai = (short)command.TrangThai,
            KyHieu = kyHieu
        };

        dbContext.KqHocTaps.Add(kqht);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(kqht).State = EntityState.Detached;
            return CreateKqhtResult.Duplicate();
        }

        return CreateKqhtResult.Success(new KqhtDto(
            kqht.MaKQHT,
            kqht.TenKQHT,
            kqht.ThuTu,
            kqht.MoTa,
            (TrangThaiSuDung)kqht.TrangThai,
            kqht.KyHieu));
    }

    private static bool IsUniqueConstraintViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }
}
