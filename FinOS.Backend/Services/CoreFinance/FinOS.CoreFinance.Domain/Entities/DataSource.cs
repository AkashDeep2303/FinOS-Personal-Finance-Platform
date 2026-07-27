namespace FinOS.CoreFinance.Domain.Entities;

public sealed class DataSource
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? InstitutionName { get; set; }
    public string ConnectionMode { get; set; } = "ManualImport";
    public string Status { get; set; } = "Active";
    public DateTime? LastImportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
