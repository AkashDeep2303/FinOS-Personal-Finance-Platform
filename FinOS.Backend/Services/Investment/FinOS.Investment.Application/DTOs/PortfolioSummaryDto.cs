using FinOS.Investment.Domain.Enums;

namespace FinOS.Investment.Application.DTOs;

public class PortfolioSummaryDto
{
    public long PortfolioId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPct { get; set; }
    public List<AssetAllocationDto> AssetAllocation { get; set; } = new();
    public List<HoldingDto> TopHoldings { get; set; } = new();
}

public class AssetAllocationDto
{
    public AssetClass AssetClass { get; set; }
    public string AssetClassName { get; set; } = string.Empty;
    public decimal InvestedAmount { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AllocationPct { get; set; }
    public decimal ReturnPct { get; set; }
}
