namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IFinancialDocumentStorage
{
    Task<string> SaveAsync(long userId, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
