namespace FinOS.CoreFinance.Application.DTOs;

public class TransactionDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public long? TransferAccountId { get; set; }
    public string? TransferAccountName { get; set; }
    public string Type { get; set; } = string.Empty;
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
    public List<string>? AttachmentUrls { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationName { get; set; }
    public string Source { get; set; } = "Manual";
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

public class TagDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class CreateTransactionRequest
{
    public long AccountId { get; set; }
    public long? CategoryId { get; set; }
    public long? TransferAccountId { get; set; }
    public string Type { get; set; } = "Expense";
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
    public List<string>? AttachmentUrls { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationName { get; set; }
    public string Source { get; set; } = "Manual";
    public List<long>? TagIds { get; set; }
}

public class UpdateTransactionRequest
{
    public long? CategoryId { get; set; }
    public long? TransferAccountId { get; set; }
    public string Type { get; set; } = "Expense";
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
    public List<string>? AttachmentUrls { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public string? LocationName { get; set; }
    public List<long>? TagIds { get; set; }
}

public class SplitTransactionRequest
{
    public List<SplitItem> Splits { get; set; } = new();
}

public class SplitItem
{
    public decimal Amount { get; set; }
    public long? CategoryId { get; set; }
    public string? Notes { get; set; }
    public List<long>? TagIds { get; set; }
}

public class TransactionFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Type { get; set; }
    public long? AccountId { get; set; }
    public long? CategoryId { get; set; }
    public string? MerchantName { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}

public class MonthlySummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount => TotalIncome - TotalExpense;
    public List<CategorySummaryDto> CategorySummaries { get; set; } = new();
}

public class CategorySummaryDto
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
