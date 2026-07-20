using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Entities;

public class DetectedSubscription : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public SubscriptionFrequency Frequency { get; set; }
    public DateTime? NextExpectedDate { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public long? LastTransactionId { get; set; }
    public decimal DetectionConfidence { get; set; }
    public int TransactionCount { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Category? Category { get; set; }
    public Transaction? LastTransaction { get; set; }
}
