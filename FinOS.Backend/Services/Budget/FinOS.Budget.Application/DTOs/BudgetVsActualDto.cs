namespace FinOS.Budget.Application.DTOs;

public class BudgetVsActualDto
{
    public long BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal OverallSpentPct { get; set; }
    public List<CategoryVsActualDto> Categories { get; set; } = new();
}

public class CategoryVsActualDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CustomLabel { get; set; }
    public decimal Allocated { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal SpentPct { get; set; }
    public bool IsOverBudget => Spent > Allocated;
    public bool IsNearLimit => SpentPct >= 80;
}
