using Dapper;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public sealed class FinancialDocumentRepository(IConnectionFactory connectionFactory) : IFinancialDocumentRepository
{
    public async Task<IReadOnlyList<FinancialDocument>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, DocumentType, Title, Issuer, FinancialYear,
                   DocumentDate, Notes, Status, OriginalFileName, ContentType,
                   FileSizeBytes, Sha256,
                   CAST(CASE WHEN StorageKey IS NULL THEN 0 ELSE 1 END AS bit) AS HasFile,
                   CreatedAt, UpdatedAt
            FROM Core.FinancialDocuments
            WHERE UserId = @UserId AND DeletedAt IS NULL
            ORDER BY COALESCE(DocumentDate, CAST(CreatedAt AS DATE)) DESC, CreatedAt DESC;
            """;
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<FinancialDocument>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<FinancialDocumentStorageMetadata?> GetStorageMetadataAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, StorageKey, OriginalFileName, ContentType, FileSizeBytes
            FROM Core.FinancialDocuments
            WHERE Id = @Id AND UserId = @UserId AND DeletedAt IS NULL;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<FinancialDocumentStorageMetadata>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> AttachFileAsync(long id, long userId, string storageKey, string originalFileName, string contentType, long fileSizeBytes, string sha256, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Core.FinancialDocuments
            SET StorageKey = @StorageKey, OriginalFileName = @OriginalFileName,
                ContentType = @ContentType, FileSizeBytes = @FileSizeBytes,
                Sha256 = @Sha256, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND UserId = @UserId AND DeletedAt IS NULL AND StorageKey IS NULL;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql,
            new { Id = id, UserId = userId, StorageKey = storageKey, OriginalFileName = originalFileName, ContentType = contentType, FileSizeBytes = fileSizeBytes, Sha256 = sha256 },
            cancellationToken: cancellationToken)) == 1;
    }

    public async Task<FinancialDocument> AddAsync(FinancialDocument document, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT Core.FinancialDocuments
                (UserId, DocumentType, Title, Issuer, FinancialYear, DocumentDate, Notes, Status)
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.DocumentType, INSERTED.Title,
                   INSERTED.Issuer, INSERTED.FinancialYear, INSERTED.DocumentDate,
                   INSERTED.Notes, INSERTED.Status, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES
                (@UserId, @DocumentType, @Title, @Issuer, @FinancialYear, @DocumentDate, @Notes, N'Recorded');
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<FinancialDocument>(
            new CommandDefinition(sql, document, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Core.FinancialDocuments
            SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), Status = N'Archived'
            WHERE Id = @Id AND UserId = @UserId AND DeletedAt IS NULL;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken)) == 1;
    }
}
