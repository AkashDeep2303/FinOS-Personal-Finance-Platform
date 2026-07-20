using FinOS.Common.Interfaces;

namespace FinOS.CoreFinance.Domain.Entities;

public class Account : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? InstitutionName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsIncludedInNetWorth { get; set; } = true;
    public bool IsSynced { get; set; }
    public string? SyncProvider { get; set; }
    public string? SyncAccountId { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public AccountType? AccountType { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
