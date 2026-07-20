using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Entities;

public class Transaction : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountId { get; set; }
    public long? CategoryId { get; set; }
    public long? TransferAccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal? ExchangeRate { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? OriginalCurrency { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }
    public TimeSpan? TransactionTime { get; set; }
    public DateTime? ValueDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? MerchantName { get; set; }
    public string? MerchantCategory { get; set; }
    public bool IsRecurring { get; set; }
    public long? RecurringScheduleId { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsSplit { get; set; }
    public long? ParentTransactionId { get; set; }
    public string? SplitNote { get; set; }
    public string? AttachmentUrls { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationName { get; set; }
    public TransactionSource Source { get; set; } = TransactionSource.Manual;
    public string? ImportBatchId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public Account? Account { get; set; }
    public Account? TransferAccount { get; set; }
    public Category? Category { get; set; }
    public RecurringSchedule? RecurringSchedule { get; set; }
    public Transaction? ParentTransaction { get; set; }
    public ICollection<Transaction> ChildTransactions { get; set; } = new List<Transaction>();
    public ICollection<TransactionTag> Tags { get; set; } = new List<TransactionTag>();
}
