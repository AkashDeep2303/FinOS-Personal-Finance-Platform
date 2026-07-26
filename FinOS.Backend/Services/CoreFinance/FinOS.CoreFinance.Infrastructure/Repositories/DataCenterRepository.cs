using Dapper;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using FinOS.Common.Helpers;
using System.Text.Json;
using System.Data;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public sealed class DataCenterRepository(IConnectionFactory connectionFactory) : IDataCenterRepository
{
    public async Task<DataCenterOverview> GetOverviewAsync(
        long userId,
        int importLimit,
        int issueLimit,
        CancellationToken cancellationToken = default)
    {
        const string summarySql = """
            SELECT
                (SELECT COUNT_BIG(1)
                 FROM Views.vw_DataQuality
                 WHERE UserId = @UserId) AS OpenIssueCount,
                (SELECT COUNT_BIG(1)
                 FROM Import.ImportErrors e
                 INNER JOIN Import.ImportBatches b ON b.Id = e.BatchId
                 WHERE b.UserId = @UserId AND e.IsResolved = 0) AS UnresolvedImportErrorCount,
                COALESCE((SELECT SUM(CAST(SuccessRows AS BIGINT))
                          FROM Import.ImportBatches
                          WHERE UserId = @UserId), 0) AS ImportedRowCount,
                COALESCE((SELECT SUM(CAST(FailedRows AS BIGINT))
                          FROM Import.ImportBatches
                          WHERE UserId = @UserId), 0) AS FailedRowCount;
            """;

        const string importsSql = """
            SELECT TOP (@ImportLimit)
                Id, Source, FileName, TotalRows, ProcessedRows, SuccessRows,
                FailedRows, DuplicateRows, Status, StartedAt, CompletedAt, CreatedAt
            FROM Import.ImportBatches
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;
            """;

        const string issuesSql = """
            SELECT TOP (@IssueLimit)
                IssueType, EntityType, EntityId, IssueDescription, IssueDetectedAt
            FROM Views.vw_DataQuality
            WHERE UserId = @UserId
            ORDER BY IssueDetectedAt DESC;
            """;

        using var connection = connectionFactory.CreateConnection();
        var parameters = new { UserId = userId, ImportLimit = importLimit, IssueLimit = issueLimit };

        var summary = await connection.QuerySingleAsync<DataCenterSummary>(
            new CommandDefinition(summarySql, parameters, cancellationToken: cancellationToken));
        var imports = (await connection.QueryAsync<ImportBatchSummary>(
            new CommandDefinition(importsSql, parameters, cancellationToken: cancellationToken))).AsList();
        var issues = (await connection.QueryAsync<DataQualityIssue>(
            new CommandDefinition(issuesSql, parameters, cancellationToken: cancellationToken))).AsList();

        var penalty = Math.Min(
            100L,
            (summary.OpenIssueCount * 5L) + (summary.UnresolvedImportErrorCount * 2L));

        return new DataCenterOverview
        {
            DataQualityScore = (int)(100L - penalty),
            ScoreCalculation = "100 minus 5 points per open data-quality issue and 2 points per unresolved import error, with a minimum score of 0.",
            OpenIssueCount = ToInt(summary.OpenIssueCount),
            UnresolvedImportErrorCount = ToInt(summary.UnresolvedImportErrorCount),
            ImportedRowCount = ToInt(summary.ImportedRowCount),
            FailedRowCount = ToInt(summary.FailedRowCount),
            RecentImports = imports,
            Issues = issues
        };
    }

    private static int ToInt(long value) => (int)Math.Min(int.MaxValue, Math.Max(0L, value));

    public async Task<IReadOnlyList<ImportReconciliationIssue>> GetReconciliationIssuesAsync(
        long userId,
        int limit,
        bool includeResolved,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                e.Id, e.BatchId, b.Source, COALESCE(b.FileName, N'Unnamed import') AS FileName,
                e.RowNumber, e.ErrorReason, e.IsResolved, e.ResolvedTransactionId, e.CreatedAt
            FROM Import.ImportErrors e
            INNER JOIN Import.ImportBatches b ON b.Id = e.BatchId
            WHERE b.UserId = @UserId
              AND (@IncludeResolved = 1 OR e.IsResolved = 0)
            ORDER BY e.IsResolved, e.CreatedAt DESC;
            """;
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<ImportReconciliationIssue>(
            new CommandDefinition(
                sql,
                new { UserId = userId, Limit = Math.Clamp(limit, 1, 200), IncludeResolved = includeResolved },
                cancellationToken: cancellationToken))).AsList();
    }

    public async Task<bool> ResolveImportErrorAsync(
        long id,
        long userId,
        long? transactionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE e
            SET IsResolved = 1, ResolvedTransactionId = @TransactionId
            FROM Import.ImportErrors e
            INNER JOIN Import.ImportBatches b ON b.Id = e.BatchId
            WHERE e.Id = @Id
              AND b.UserId = @UserId
              AND e.IsResolved = 0
              AND
              (
                  @TransactionId IS NULL
                  OR EXISTS
                  (
                      SELECT 1
                      FROM Core.Transactions t
                      WHERE t.Id = @TransactionId
                        AND t.UserId = @UserId
                        AND t.DeletedAt IS NULL
                  )
              );
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id, UserId = userId, TransactionId = transactionId },
                cancellationToken: cancellationToken)) == 1;
    }

    public async Task<ImportDuplicateAnalysis> CheckImportDuplicatesAsync(
        long userId,
        long accountId,
        IReadOnlyList<CsvTransactionCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1 FROM Core.Accounts
                WHERE Id = @AccountId AND UserId = @UserId AND DeletedAt IS NULL AND IsActive = 1
            ) THEN 1 ELSE 0 END AS BIT);

            WITH CandidateRows AS
            (
                SELECT RowNumber, TransactionDate, [Description], Amount, [Type], ReferenceNumber
                FROM OPENJSON(@Candidates)
                WITH
                (
                    RowNumber INT '$.RowNumber',
                    TransactionDate DATE '$.TransactionDate',
                    [Description] NVARCHAR(500) '$.Description',
                    Amount DECIMAL(18,2) '$.Amount',
                    [Type] NVARCHAR(20) '$.Type',
                    ReferenceNumber NVARCHAR(100) '$.ReferenceNumber'
                )
            )
            SELECT c.RowNumber, MIN(t.Id) AS ExistingTransactionId,
                   CASE WHEN c.ReferenceNumber IS NOT NULL AND MAX(CASE WHEN t.ReferenceNumber = c.ReferenceNumber THEN 1 ELSE 0 END) = 1
                        THEN N'ReferenceNumber' ELSE N'DateAmountTypeDescription' END AS MatchReason
            FROM CandidateRows c
            INNER JOIN Core.Transactions t
                ON t.UserId = @UserId AND t.AccountId = @AccountId AND t.DeletedAt IS NULL
               AND
               (
                   (c.ReferenceNumber IS NOT NULL AND t.ReferenceNumber = c.ReferenceNumber)
                   OR
                   (t.TransactionDate = c.TransactionDate AND t.Amount = c.Amount
                    AND t.Type = c.Type AND t.Description = c.Description)
               )
            GROUP BY c.RowNumber, c.ReferenceNumber
            ORDER BY c.RowNumber;
            """;
        using var connection = connectionFactory.CreateConnection();
        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new { UserId = userId, AccountId = accountId, Candidates = JsonSerializer.Serialize(candidates) },
                cancellationToken: cancellationToken));
        var accountExists = await grid.ReadSingleAsync<bool>();
        var matches = (await grid.ReadAsync<ImportDuplicateMatch>()).AsList();
        return new ImportDuplicateAnalysis
        {
            AccountExists = accountExists,
            CandidateRows = candidates.Count,
            DuplicateRows = matches.Count,
            Matches = matches.Take(100).ToList()
        };
    }
    public async Task<CsvImportResult> ImportTransactionsAsync(long userId,long accountId,string fileName,string columnMapping,IReadOnlyList<CsvTransactionCandidate> candidates,string duplicatePolicy,CancellationToken cancellationToken=default)
    {
        using var connection=connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<CsvImportResult>(new CommandDefinition("Core.sp_ImportTransactions",new{UserId=userId,AccountId=accountId,FileName=fileName,ColumnMapping=columnMapping,Rows=JsonSerializer.Serialize(candidates),DuplicatePolicy=duplicatePolicy},commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken));
    }

    private sealed class DataCenterSummary
    {
        public long OpenIssueCount { get; init; }
        public long UnresolvedImportErrorCount { get; init; }
        public long ImportedRowCount { get; init; }
        public long FailedRowCount { get; init; }
    }
}
