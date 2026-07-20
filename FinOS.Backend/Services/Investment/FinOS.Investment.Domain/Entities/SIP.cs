using FinOS.Investment.Domain.Enums;
using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Entities;

public class SIP : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long HoldingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public SIPFrequency Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextExecutionDate { get; set; }
    public DateTime? LastExecutedDate { get; set; }
    public long? SourceAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal TotalInvested { get; set; }
    public int InstallmentsDone { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Holding Holding { get; set; } = null!;
}
