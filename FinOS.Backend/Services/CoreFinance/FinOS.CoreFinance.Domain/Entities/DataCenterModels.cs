namespace FinOS.CoreFinance.Domain.Entities;

public sealed class ImportBatchSummary
{
    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int ProcessedRows { get; init; }
    public int SuccessRows { get; init; }
    public int FailedRows { get; init; }
    public int DuplicateRows { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DataQualityIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string IssueDescription { get; init; } = string.Empty;
    public DateTime IssueDetectedAt { get; init; }
}

public sealed class DataCenterOverview
{
    public int DataQualityScore { get; init; }
    public string ScoreCalculation { get; init; } = string.Empty;
    public int OpenIssueCount { get; init; }
    public int UnresolvedImportErrorCount { get; init; }
    public int ImportedRowCount { get; init; }
    public int FailedRowCount { get; init; }
    public IReadOnlyList<ImportBatchSummary> RecentImports { get; init; } = [];
    public IReadOnlyList<DataQualityIssue> Issues { get; init; } = [];
}

public sealed class ImportReconciliationIssue
{
    public long Id { get; init; }
    public Guid BatchId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int RowNumber { get; init; }
    public string ErrorReason { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public long? ResolvedTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ImportDuplicateMatch
{
    public int RowNumber { get; init; }
    public long? ExistingTransactionId { get; init; }
    public int? MatchingRowNumber { get; init; }
    public string MatchReason { get; init; } = string.Empty;
}

public sealed class ImportDuplicateAnalysis
{
    public bool AccountExists { get; init; }
    public int CandidateRows { get; init; }
    public int DuplicateRows { get; init; }
    public IReadOnlyList<ImportDuplicateMatch> Matches { get; init; } = [];
}
public sealed class CsvImportResult
{
 public Guid BatchId { get; init; }
 public int TotalRows { get; init; }
 public int ImportedRows { get; init; }
 public int DuplicateRows { get; init; }
 public decimal BalanceDelta { get; init; }
}
