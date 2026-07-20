using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Entities;

public class RecurringSchedule : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountId { get; set; }
    public long? CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public RecurringFrequency Frequency { get; set; }
    public int IntervalValue { get; set; } = 1;
    public int? DayOfMonth { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextOccurrenceDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoCreate { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Account? Account { get; set; }
    public Category? Category { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
