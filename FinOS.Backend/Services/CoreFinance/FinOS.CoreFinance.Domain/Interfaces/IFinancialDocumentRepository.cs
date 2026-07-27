using FinOS.CoreFinance.Domain.Entities;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IFinancialDocumentRepository
{
    Task<IReadOnlyList<FinancialDocument>> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task<FinancialDocument> AddAsync(FinancialDocument document, CancellationToken cancellationToken = default);
    Task<FinancialDocumentStorageMetadata?> GetStorageMetadataAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<bool> AttachFileAsync(long id, long userId, string storageKey, string originalFileName, string contentType, long fileSizeBytes, string sha256, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
}
