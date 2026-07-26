using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public record GetInvestmentPerformanceQuery(long UserId, long PortfolioId, int Months)
    : IRequest<InvestmentPerformanceDto>;

public class GetInvestmentPerformanceQueryHandler : IRequestHandler<GetInvestmentPerformanceQuery, InvestmentPerformanceDto>
{
    private readonly IPortfolioRepository _portfolios;
    private readonly IInvestmentAnalyticsRepository _analytics;
    public GetInvestmentPerformanceQueryHandler(IPortfolioRepository portfolios, IInvestmentAnalyticsRepository analytics)
        => (_portfolios, _analytics) = (portfolios, analytics);

    public async Task<InvestmentPerformanceDto> Handle(GetInvestmentPerformanceQuery query, CancellationToken ct)
    {
        var portfolio = await _portfolios.GetByIdAsync(query.PortfolioId, ct)
            ?? throw new NotFoundException("Portfolio", query.PortfolioId);
        if (portfolio.UserId != query.UserId) throw new UnauthorizedAccessException();
        var result = await _analytics.GetPerformanceAsync(query.PortfolioId, ct);
        var trend = await _analytics.GetContributionTrendAsync(query.PortfolioId, query.Months, ct);
        var history = await _analytics.GetValueHistoryAsync(query.PortfolioId, query.Months, ct);
        var returnPct = result.InvestedValue == 0 ? 0 :
            Math.Round((result.UnrealizedGain + result.RealizedGain + result.DividendIncome) / result.InvestedValue * 100, 2);
        return new(query.PortfolioId, result.InvestedValue, result.CurrentValue, result.UnrealizedGain,
            result.RealizedGain, result.DividendIncome, result.Charges, returnPct,
            result.SellTransactionCount == result.ValuedSellTransactionCount,
            trend.Select(x => new ContributionTrendDto(x.YearMonth, x.Contributions, x.Withdrawals, x.Income)).ToList(),
            history.Select(x => new PortfolioValuePointDto(x.SnapshotDate, x.InvestedValue, x.CurrentValue, x.UnrealizedGain)).ToList());
    }
}
