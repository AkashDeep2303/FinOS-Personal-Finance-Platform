using FinOS.Investment.Domain.Enums;
using FinOS.Common.Interfaces;

namespace FinOS.Investment.Domain.Entities;

public class Holding : IAuditableEntity, ISoftDeletable
{
    public long Id { get; set; }
    public long PortfolioId { get; set; }
    public long InvestmentTypeId { get; set; }
    public string? Symbol { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AvgPurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePct { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPct { get; set; }
    public decimal? XIRR { get; set; }
    public decimal? CAGR { get; set; }
    public decimal DividendReceived { get; set; }
    public string? FundHouse { get; set; }
    public FundCategory? FundCategory { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime? LockInEndDate { get; set; }
    public DateTime? NAVDate { get; set; }
    public DateTime? LastPriceUpdateAt { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public Portfolio Portfolio { get; set; } = null!;
    public List<InvestmentTransaction> InvestmentTransactions { get; set; } = new();
}
