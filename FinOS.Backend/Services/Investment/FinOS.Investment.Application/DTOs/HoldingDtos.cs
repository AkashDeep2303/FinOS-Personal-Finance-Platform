using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Application.DTOs;

public class HoldingDto
{
    public long Id { get; set; }
    public long PortfolioId { get; set; }
    public long InvestmentTypeId { get; set; }
    public string? InvestmentTypeName { get; set; }
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
    public bool IsActive { get; set; }
}

public class CreateHoldingRequest
{
    public long PortfolioId { get; set; }
    public long InvestmentTypeId { get; set; }
    public string? Symbol { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AvgPurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? FundHouse { get; set; }
    public FundCategory? FundCategory { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public DateTime? MaturityDate { get; set; }
    public decimal? InterestRate { get; set; }
    public DateTime? LockInEndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateHoldingPriceRequest
{
    public decimal CurrentPrice { get; set; }
    public DateTime? NAVDate { get; set; }
}
