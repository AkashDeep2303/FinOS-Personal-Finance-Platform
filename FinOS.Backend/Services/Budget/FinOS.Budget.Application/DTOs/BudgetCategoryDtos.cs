namespace FinOS.Budget.Application.DTOs;

public class BudgetCategoryDto
{
    public long Id { get; set; }
    public long BudgetId { get; set; }
    public long CategoryId { get; set; }
    public string? CustomLabel { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;
    public decimal SpentPercentage => AllocatedAmount > 0 ? Math.Round(SpentAmount / AllocatedAmount * 100, 2) : 0;
    public decimal AlertThresholdPct { get; set; }
    public int SortOrder { get; set; }
}

public class CreateBudgetCategoryRequest
{
    public long CategoryId { get; set; }
    public string? CustomLabel { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal AlertThresholdPct { get; set; } = 80m;
    public int SortOrder { get; set; }
}

public class UpdateBudgetCategoryRequest
{
    public decimal? AllocatedAmount { get; set; }
    public string? CustomLabel { get; set; }
    public decimal? AlertThresholdPct { get; set; }
    public int? SortOrder { get; set; }
}
