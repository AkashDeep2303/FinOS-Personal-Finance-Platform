using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Enums;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public class GetPortfolioSummaryQuery : IRequest<PortfolioSummaryDto>
{
    public long PortfolioId { get; set; }

    public GetPortfolioSummaryQuery(long portfolioId)
    {
        PortfolioId = portfolioId;
    }
}

public class GetPortfolioSummaryQueryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInvestmentTypeRepository _investmentTypeRepository;

    public GetPortfolioSummaryQueryHandler(
        IPortfolioRepository portfolioRepository,
        IInvestmentTypeRepository investmentTypeRepository)
    {
        _portfolioRepository = portfolioRepository;
        _investmentTypeRepository = investmentTypeRepository;
    }

    public async Task<PortfolioSummaryDto> Handle(GetPortfolioSummaryQuery query, CancellationToken ct)
    {
        var portfolio = await _portfolioRepository.GetWithHoldingsAsync(query.PortfolioId, ct);

        if (portfolio == null)
            return null!;

        var activeHoldings = portfolio.Holdings.Where(h => h.IsActive && h.DeletedAt == null).ToList();
        var totalInvested = activeHoldings.Sum(h => h.InvestedAmount);
        var currentValue = activeHoldings.Sum(h => h.CurrentValue);
        var totalReturn = currentValue - totalInvested;
        var totalReturnPct = totalInvested > 0 ? Math.Round(totalReturn / totalInvested * 100, 2) : 0;
        var investmentTypes = (await _investmentTypeRepository.GetAllAsync(ct)).ToDictionary(x => x.Id);

        var assetAllocation = activeHoldings
            .GroupBy(h => investmentTypes.TryGetValue(h.InvestmentTypeId, out var type)
                ? new { type.AssetClass, type.Name }
                : new { AssetClass = AssetClass.Hybrid, Name = "Other" })
            .Select(g =>
            {
                var invested = g.Sum(h => h.InvestedAmount);
                var current = g.Sum(h => h.CurrentValue);
                return new AssetAllocationDto
                {
                    AssetClass = g.Key.AssetClass,
                    AssetClassName = g.Key.Name,
                    InvestedAmount = invested,
                    CurrentValue = current,
                    AllocationPct = currentValue > 0 ? Math.Round(current / currentValue * 100, 2) : 0,
                    ReturnPct = invested > 0 ? Math.Round((current - invested) / invested * 100, 2) : 0
                };
            }).ToList();

        return new PortfolioSummaryDto
        {
            PortfolioId = portfolio.Id,
            PortfolioName = portfolio.Name,
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            TotalReturn = totalReturn,
            TotalReturnPct = totalReturnPct,
            LargestHoldingPct = currentValue > 0
                ? Math.Round(activeHoldings.Max(h => h.CurrentValue) / currentValue * 100, 2) : 0,
            ConcentratedHoldingCount = currentValue > 0
                ? activeHoldings.Count(h => h.CurrentValue / currentValue >= 0.20m) : 0,
            AssetAllocation = assetAllocation,
            TopHoldings = activeHoldings
                .OrderByDescending(h => h.CurrentValue)
                .Take(10)
                .Select(h => new HoldingDto
                {
                    Id = h.Id, PortfolioId = h.PortfolioId, InvestmentTypeId = h.InvestmentTypeId,
                    Symbol = h.Symbol, Name = h.Name, Quantity = h.Quantity,
                    AvgPurchasePrice = h.AvgPurchasePrice, CurrentPrice = h.CurrentPrice,
                    CurrentValue = h.CurrentValue, InvestedAmount = h.InvestedAmount,
                    DayChange = h.DayChange, DayChangePct = h.DayChangePct,
                    TotalReturn = h.TotalReturn, TotalReturnPct = h.TotalReturnPct,
                    XIRR = h.XIRR, CAGR = h.CAGR, DividendReceived = h.DividendReceived,
                    FundHouse = h.FundHouse, FundCategory = h.FundCategory, RiskLevel = h.RiskLevel,
                    MaturityDate = h.MaturityDate, InterestRate = h.InterestRate,
                    LockInEndDate = h.LockInEndDate, IsActive = h.IsActive
                }).ToList()
        };
    }
}
