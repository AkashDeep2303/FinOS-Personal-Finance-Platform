namespace FinOS.CoreFinance.Application.DTOs;

public class AccountTypeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class AccountDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountTypeId { get; set; }
    public string AccountTypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? InstitutionName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsIncludedInNetWorth { get; set; }
    public bool IsSynced { get; set; }
    public string? SyncProvider { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAccountRequest
{
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
    public string? Notes { get; set; }
}

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? InstitutionName { get; set; }
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsIncludedInNetWorth { get; set; } = true;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
