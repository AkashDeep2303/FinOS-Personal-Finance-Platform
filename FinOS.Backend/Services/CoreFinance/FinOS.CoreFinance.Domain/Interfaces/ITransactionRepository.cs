using FinOS.Common.Models;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Domain.Interfaces;

public interface ITransactionRepository : Common.Interfaces.IRepository<Entities.Transaction>
{
    Task<PagedResult<Entities.Transaction>> GetByDateRangeAsync(
        long userId, DateTime startDate, DateTime endDate,
        PagedQuery query, TransactionType? type = null,
        long? accountId = null, long? categoryId = null,
        string? merchantName = null, CancellationToken ct = default);

    Task<MonthlySummaryData> GetMonthlySummaryAsync(
        long userId, int year, int month, CancellationToken ct = default);

    Task<List<Entities.Transaction>> GetByMerchantNameAsync(
        long userId, string merchantName, CancellationToken ct = default);
}

public class MonthlySummaryData
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount => TotalIncome - TotalExpense;
    public List<CategorySummaryData> CategorySummaries { get; set; } = new();
}

public class CategorySummaryData
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
