using FinOS.Budget.Domain.Enums;
using FinOS.Common.Interfaces;

namespace FinOS.Budget.Domain.Entities;

public class SavingsRule : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public SavingsRuleType RuleType { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? TargetAccountId { get; set; }
    public long? SourceAccountId { get; set; }
    public decimal? RoundUpTo { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public SavingsFrequency Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal TotalSaved { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
