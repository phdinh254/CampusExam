using System.Text.RegularExpressions;
using CampusExam.Application.Common;

namespace CampusExam.Infrastructure.Storage;

/// <summary>Lưu file trong một thư mục cục bộ (P0, một máy chủ). Thư mục cấu hình ở <c>Storage:Root</c>.</summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var path = DuongDan(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // FileMode.CreateNew: key bất biến, không bao giờ ghi đè file đã có.
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = DuongDan(key);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(DuongDan(key));
        return Task.CompletedTask;
    }

    public IEnumerable<string> ListOlderThan(string prefix, DateTime before)
    {
        var directory = DuongDan(prefix);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(path => File.GetLastWriteTimeUtc(path) < before)
            .Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/'))
            .ToList();
    }

    /// <summary>Chặn key lạ (.., đường dẫn tuyệt đối) để không đọc/ghi ra ngoài thư mục gốc.</summary>
    private string DuongDan(string key)
    {
        if (!KeyHopLe().IsMatch(key) || key.Split('/').Any(phan => phan is "" or "." or ".."))
        {
            throw new ArgumentException($"Storage key không hợp lệ: {key}", nameof(key));
        }

        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Storage key không hợp lệ: {key}", nameof(key));
        }

        return path;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9./-]{0,499}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyHopLe();
}
