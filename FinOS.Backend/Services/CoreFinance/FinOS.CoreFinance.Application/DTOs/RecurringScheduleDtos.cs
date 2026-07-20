namespace FinOS.CoreFinance.Application.DTOs;

public class RecurringScheduleDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int IntervalValue { get; set; }
    public int? DayOfMonth { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextOccurrenceDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public bool IsActive { get; set; }
    public bool AutoCreate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRecurringScheduleRequest
{
    public long AccountId { get; set; }
    public long? CategoryId { get; set; }
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Frequency { get; set; } = "Monthly";
    public int IntervalValue { get; set; } = 1;
    public int? DayOfMonth { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool AutoCreate { get; set; }
}
