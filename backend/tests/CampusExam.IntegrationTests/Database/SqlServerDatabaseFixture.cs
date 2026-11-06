using System.Text.RegularExpressions;
using CampusExam.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusExam.IntegrationTests.Database;

/// <summary>
/// Tạo database SQL Server tạm từ database/CampusExamDB.sql cho mỗi lần chạy test và xóa khi xong.
/// Máy chủ lấy từ biến môi trường CAMPUSEXAM_TEST_SQLSERVER, mặc định SQL Server cục bộ với Windows auth.
/// </summary>
public sealed partial class SqlServerDatabaseFixture : IAsyncLifetime
{
    private const string ServerVariable = "CAMPUSEXAM_TEST_SQLSERVER";
    private const string DefaultServer =
        "Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private const string DatabasePrefix = "CampusExam_Test_";

    // Xóa con trước cha; MUCDO là danh mục seed nên giữ lại.
    private static readonly string[] DataTables =
    [
        "TRA_LOI", "BAILAM", "PHANCONGTHI", "KYTHI", "HOAN_VI", "DECON",
        "TAINGUYEN_GOMCAU", "GOM_CAU", "DEGOC", "CAU_TRUC", "MATRAN",
        "TAINGUYEN_CAUHOI", "CAUHOI", "KQHOCTAP", "THISINH", "LOINHAPDULIEU",
        "LANIMPORT", "NHATKYHD", "PHIENDANGNHAP", "TAIKHOAN"
    ];

    private readonly string _serverConnectionString;

    public SqlServerDatabaseFixture()
    {
        _serverConnectionString =
            Environment.GetEnvironmentVariable(ServerVariable) ?? DefaultServer;
        DatabaseName = $"{DatabasePrefix}{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..48];
        ConnectionString = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = DatabaseName
        }.ConnectionString;
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    public CampusExamDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CampusExamDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        var sql = string.Join(
            Environment.NewLine,
            DataTables.Select(table => $"DELETE FROM dbo.[{table}];"));
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_serverConnectionString);
        try
        {
            await connection.OpenAsync();
        }
        catch (SqlException exception)
        {
            throw new InvalidOperationException(
                $"Không kết nối được SQL Server cho integration test. Đặt biến môi trường {ServerVariable} " +
                "bằng chuỗi kết nối tới máy chủ (không cần Database).",
                exception);
        }

        await DropStaleTestDatabasesAsync(connection);

        var script = (await File.ReadAllTextAsync(FindSchemaScript()))
            .Replace("[CampusExam]", $"[{DatabaseName}]", StringComparison.Ordinal)
            .Replace("N'CampusExam'", $"N'{DatabaseName}'", StringComparison.Ordinal);

        foreach (var batch in BatchSeparator().Split(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await DropDatabaseAsync(connection, DatabaseName);
    }

    private static async Task DropStaleTestDatabasesAsync(SqlConnection connection)
    {
        // Database còn sót từ lần chạy bị ngắt giữa chừng; bỏ qua database vừa tạo của lần chạy song song khác.
        var stale = new List<string>();
        await using (var command = new SqlCommand(
            "SELECT name FROM sys.databases WHERE name LIKE @prefix AND create_date < DATEADD(HOUR, -2, GETDATE());",
            connection))
        {
            command.Parameters.AddWithValue("@prefix", DatabasePrefix + "%");
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stale.Add(reader.GetString(0));
            }
        }

        foreach (var name in stale)
        {
            await DropDatabaseAsync(connection, name);
        }
    }

    private static async Task DropDatabaseAsync(SqlConnection connection, string name)
    {
        var quoted = "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
        await using var command = new SqlCommand(
            $"""
            IF DB_ID(@name) IS NOT NULL
            BEGIN
                ALTER DATABASE {quoted} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {quoted};
            END;
            """,
            connection);
        command.Parameters.AddWithValue("@name", name);
        await command.ExecuteNonQueryAsync();
    }

    private static string FindSchemaScript()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "database", "CampusExamDB.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Không tìm thấy database/CampusExamDB.sql từ thư mục chạy test.");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BatchSeparator();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}
