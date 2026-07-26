using FinOS.CoreFinance.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinOS.CoreFinance.Infrastructure.Storage;

public sealed class LocalFinancialDocumentStorage : IFinancialDocumentStorage
{
    private readonly string _root;

    public LocalFinancialDocumentStorage(IConfiguration configuration)
    {
        var configured = configuration["DocumentStorage:RootPath"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "private-documents")
            : configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(long userId, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var key = Path.Combine(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), $"{Guid.NewGuid():N}.bin");
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
        return key.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
        File.ReadAllBytesAsync(Resolve(storageKey), cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid document storage key.");
        return candidate;
    }
}
