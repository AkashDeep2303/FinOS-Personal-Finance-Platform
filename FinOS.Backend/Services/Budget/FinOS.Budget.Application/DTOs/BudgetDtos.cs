using FinOS.Budget.Domain.Enums;

namespace FinOS.Budget.Application.DTOs;

public class BudgetDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; }
    public string PeriodTypeDisplay { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalBudgetAmount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal SpentPercentage { get; set; }
    public string Currency { get; set; } = "INR";
    public bool RolloverEnabled { get; set; }
    public decimal AlertThresholdPct { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BudgetCategoryDto> Categories { get; set; } = new();
}

public class CreateBudgetRequest
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal TotalBudgetAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public bool RolloverEnabled { get; set; }
    public decimal AlertThresholdPct { get; set; } = 80m;
    public bool IsTemplate { get; set; }
    public List<CreateBudgetCategoryRequest> Categories { get; set; } = new();
}

public class UpdateBudgetRequest
{
    public string? Name { get; set; }
    public PeriodType? PeriodType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? TotalBudgetAmount { get; set; }
    public string? Currency { get; set; }
    public bool? RolloverEnabled { get; set; }
    public decimal? AlertThresholdPct { get; set; }
    public bool? IsActive { get; set; }
    public List<CreateBudgetCategoryRequest>? Categories { get; set; }
}

public class BudgetListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PeriodType PeriodType { get; set; }
    public string PeriodTypeDisplay { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalBudgetAmount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public decimal SpentPercentage { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; }
    public int CategoryCount { get; set; }
}
