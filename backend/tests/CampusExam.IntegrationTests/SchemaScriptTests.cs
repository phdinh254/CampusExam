using CampusExam.IntegrationTests.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace CampusExam.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class SchemaScriptTests(SqlServerDatabaseFixture database)
{
    [Fact]
    public async Task Script_creates_all_tables_with_clustered_keys_and_trusted_constraints()
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();

        Assert.Equal(22, await ScalarAsync(connection, "SELECT COUNT(*) FROM sys.tables;"));
        Assert.Equal(0, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM sys.tables t WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = t.object_id AND i.type = 1);"));
        Assert.Equal(0, await ScalarAsync(connection,
            "SELECT (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_not_trusted = 1) + (SELECT COUNT(*) FROM sys.check_constraints WHERE is_not_trusted = 1);"));
    }

    [Fact]
    public async Task Ef_model_columns_match_database_columns()
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        var columns = new Dictionary<(string Table, string Column), (string Type, bool Nullable)>();
        await using (var command = new SqlCommand(
            """
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo';
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var type = reader.GetString(2);
                if (!reader.IsDBNull(3))
                {
                    var length = reader.GetInt32(3);
                    type += length == -1 ? "(max)" : $"({length})";
                }

                columns[(reader.GetString(0), reader.GetString(1))] = (type, reader.GetString(4) == "YES");
            }
        }

        await using var dbContext = database.CreateDbContext();
        var mismatches = new List<string>();
        foreach (var entity in dbContext.Model.GetEntityTypes())
        {
            var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(table)!;
                if (!columns.TryGetValue((table.Name, column), out var actual))
                {
                    mismatches.Add($"{table.Name}.{column}: không có trong database");
                    continue;
                }

                // INFORMATION_SCHEMA báo cột rowversion bằng tên cũ "timestamp".
                var expectedType = property.GetColumnType(table) is "rowversion" ? "timestamp" : property.GetColumnType(table);
                if (!string.Equals(expectedType, actual.Type, StringComparison.OrdinalIgnoreCase))
                {
                    mismatches.Add($"{table.Name}.{column}: EF {expectedType}, database {actual.Type}");
                }

                if (property.IsNullable != actual.Nullable)
                {
                    mismatches.Add($"{table.Name}.{column}: EF nullable={property.IsNullable}, database nullable={actual.Nullable}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    /// <summary>D-CONGBO-01: chỉ công bố kết quả kỳ thi đã kết thúc, luôn kèm người công bố.</summary>
    [Theory]
    [InlineData(0, true, false)]  // nháp
    [InlineData(4, true, false)]  // đã hủy
    [InlineData(3, false, false)] // đã kết thúc nhưng thiếu người công bố
    [InlineData(3, true, true)]
    public async Task Exam_result_can_only_be_published_after_exam_ends(byte trangThai, bool coNguoiCongBo, bool hopLe)
    {
        await database.ResetAsync();
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"""
            DECLARE @mtk int;
            INSERT dbo.TAIKHOAN (TENDN, MATKHAUHASH, VAITRO, HOTEN, TRANGTHAI, NGAYTAO)
            VALUES ('admin', 'x', 'ADMIN', N'Quản trị', 1, SYSUTCDATETIME());
            SET @mtk = SCOPE_IDENTITY();
            INSERT dbo.MATRAN (TENMT, TONGSOCAU, THOIGIANLAM, TRANGTHAIMATRAN, NGAYTAO)
            VALUES (N'Ma trận', 1, 45, 1, SYSUTCDATETIME());
            INSERT dbo.DEGOC (MTK, MAMT, MADEGOCHIENTHI, TONGSOCAU, NGAYTAO)
            VALUES (@mtk, SCOPE_IDENTITY(), 'DE-1', 1, SYSUTCDATETIME());
            INSERT dbo.KYTHI (MTK, MADE, TENKT, MONTHI, TGBATDAU, THOILUONG, HIENDIEM, CACHPHANDE,
                TRANGTHAIKYTHI, NGAYTAO, NGAYPHATHANH, SOPHUTCHOVAOTRE, CHOXEMLAI)
            VALUES (@mtk, SCOPE_IDENTITY(), N'Giữa kỳ', N'CSDL', SYSUTCDATETIME(), 45, 1, 0,
                {trangThai}, SYSUTCDATETIME(), SYSUTCDATETIME(), 10, 0);
            UPDATE dbo.KYTHI SET NGAYCONGBO = SYSUTCDATETIME(), MTKCONGBO = {(coNguoiCongBo ? "@mtk" : "NULL")};
            """,
            connection);

        if (hopLe)
        {
            await command.ExecuteNonQueryAsync();
            Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM dbo.KYTHI WHERE NGAYCONGBO IS NOT NULL;"));
        }
        else
        {
            var exception = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
            Assert.Contains("CKT_KYTHI_CONGBO", exception.Message);
        }
    }

    private static async Task<int> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
