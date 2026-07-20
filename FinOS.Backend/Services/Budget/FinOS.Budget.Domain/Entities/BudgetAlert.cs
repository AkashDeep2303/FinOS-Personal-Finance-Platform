using FinOS.Budget.Domain.Enums;

namespace FinOS.Budget.Domain.Entities;

public class BudgetAlert
{
    public long Id { get; set; }
    public long BudgetCategoryId { get; set; }
    public AlertType AlertType { get; set; }
    public decimal ThresholdPercentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public BudgetCategory BudgetCategory { get; set; } = null!;
}
