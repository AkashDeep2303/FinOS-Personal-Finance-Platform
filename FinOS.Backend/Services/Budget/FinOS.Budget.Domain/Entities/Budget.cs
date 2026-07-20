using FinOS.Budget.Domain.Enums;
using FinOS.Common.Interfaces;

namespace FinOS.Budget.Domain.Entities;

public class Budget : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalBudgetAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public bool RolloverEnabled { get; set; }
    public decimal AlertThresholdPct { get; set; } = 80m;
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public List<BudgetCategory> Categories { get; set; } = new();
}
