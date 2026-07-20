using FinOS.Budget.Domain.Enums;

namespace FinOS.Budget.Application.DTOs;

public class BudgetAlertDto
{
    public long Id { get; set; }
    public long BudgetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public string AlertTypeDisplay { get; set; } = string.Empty;
    public decimal ThresholdPercentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
