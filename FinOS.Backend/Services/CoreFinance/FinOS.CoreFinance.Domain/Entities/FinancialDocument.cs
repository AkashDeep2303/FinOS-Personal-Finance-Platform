namespace FinOS.CoreFinance.Domain.Entities;

public sealed class FinancialDocument
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? FinancialYear { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Recorded";
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public bool HasFile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FinancialDocumentStorageMetadata
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? StorageKey { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
}
