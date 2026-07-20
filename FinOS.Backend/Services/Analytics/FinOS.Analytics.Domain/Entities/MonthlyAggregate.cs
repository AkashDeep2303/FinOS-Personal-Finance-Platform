using FinOS.Common.Interfaces;

namespace FinOS.Analytics.Domain.Entities;

public class MonthlyAggregate : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int YearMonth { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalSavings { get; set; }
    public decimal SavingsRate { get; set; }
    public string? TopExpenseCategory { get; set; }
    public decimal TopExpenseAmount { get; set; }
    public int TransactionCount { get; set; }
    public string? CategoryBreakdown { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
