using FinOS.CoreFinance.Domain.Entities;
using FinOS.Common.Helpers;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface IDataCenterRepository
{
    Task<DataCenterOverview> GetOverviewAsync(
        long userId,
        int importLimit,
        int issueLimit,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportReconciliationIssue>> GetReconciliationIssuesAsync(
        long userId,
        int limit,
        bool includeResolved,
        CancellationToken cancellationToken = default);
    Task<bool> ResolveImportErrorAsync(
        long id,
        long userId,
        long? transactionId,
        CancellationToken cancellationToken = default);
    Task<ImportDuplicateAnalysis> CheckImportDuplicatesAsync(
        long userId,
        long accountId,
        IReadOnlyList<CsvTransactionCandidate> candidates,
        CancellationToken cancellationToken = default);
    Task<CsvImportResult> ImportTransactionsAsync(long userId,long accountId,string fileName,string columnMapping,IReadOnlyList<CsvTransactionCandidate> candidates,string duplicatePolicy,CancellationToken cancellationToken=default);
}
