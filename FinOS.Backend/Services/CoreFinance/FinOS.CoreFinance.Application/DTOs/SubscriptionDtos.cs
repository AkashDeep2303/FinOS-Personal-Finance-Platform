namespace FinOS.CoreFinance.Application.DTOs;

public class SubscriptionDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Frequency { get; set; } = string.Empty;
    public DateTime? NextExpectedDate { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public long? LastTransactionId { get; set; }
    public decimal DetectionConfidence { get; set; }
    public int TransactionCount { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfirmSubscriptionRequest
{
    public bool IsConfirmed { get; set; } = true;
    public long? CategoryId { get; set; }
}
