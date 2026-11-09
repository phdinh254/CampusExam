using Microsoft.Data.SqlClient;

namespace CampusExam.Infrastructure.Persistence;

internal static class SqlLoi
{
    /// <summary>Vi phạm UNIQUE/PRIMARY KEY (2601, 2627), kể cả khi bọc trong DbUpdateException.</summary>
    public static bool LaTrungKhoa(Exception exception) => CoMaLoi(exception, 2601, 2627);

    /// <summary>Vi phạm FOREIGN KEY hoặc CHECK (547).</summary>
    public static bool LaViPhamRangBuoc(Exception exception) => CoMaLoi(exception, 547);

    private static bool CoMaLoi(Exception exception, params int[] maLoi)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && maLoi.Contains(sql.Number))
            {
                return true;
            }
        }

        return false;
    }
}
