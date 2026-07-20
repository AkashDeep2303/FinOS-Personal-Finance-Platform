using FinOS.Budget.Domain.Enums;

namespace FinOS.Budget.Application.DTOs;

public class SavingsRuleDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public SavingsRuleType RuleType { get; set; }
    public string RuleTypeDisplay { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? TargetAccountId { get; set; }
    public long? SourceAccountId { get; set; }
    public decimal? RoundUpTo { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public SavingsFrequency Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalSaved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSavingsRuleRequest
{
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
}

public class UpdateSavingsRuleRequest
{
    public string? Name { get; set; }
    public long? TargetAccountId { get; set; }
    public long? SourceAccountId { get; set; }
    public decimal? RoundUpTo { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public SavingsFrequency? Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public bool? IsActive { get; set; }
}
