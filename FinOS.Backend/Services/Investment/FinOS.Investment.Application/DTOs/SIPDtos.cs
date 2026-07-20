using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Application.DTOs;

public class SIPDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long HoldingId { get; set; }
    public string HoldingName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public SIPFrequency Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextExecutionDate { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalInvested { get; set; }
    public int InstallmentsDone { get; set; }
}

public class CreateSIPRequest
{
    public long UserId { get; set; }
    public long HoldingId { get; set; }
    public decimal Amount { get; set; }
    public SIPFrequency Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public long? SourceAccountId { get; set; }
}
