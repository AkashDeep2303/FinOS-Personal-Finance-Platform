using FinOS.Common.Interfaces;

namespace FinOS.Budget.Domain.Entities;

public class BudgetCategory : IAuditableEntity
{
    public long Id { get; set; }
    public long BudgetId { get; set; }
    public long CategoryId { get; set; }
    public string? CustomLabel { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal AlertThresholdPct { get; set; } = 80m;
    public int SortOrder { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Budget Budget { get; set; } = null!;
    public List<BudgetAlert> Alerts { get; set; } = new();
}
