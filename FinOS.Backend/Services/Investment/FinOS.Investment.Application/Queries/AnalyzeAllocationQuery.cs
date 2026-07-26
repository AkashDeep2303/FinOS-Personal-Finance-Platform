using FinOS.Common.Exceptions;
using FinOS.Investment.Application.DTOs;
using FinOS.Investment.Domain.Interfaces;
using MediatR;

namespace FinOS.Investment.Application.Queries;

public record AnalyzeAllocationQuery(long UserId, AllocationAnalysisRequest Request)
    : IRequest<AllocationAnalysisDto>;

public class AnalyzeAllocationQueryHandler : IRequestHandler<AnalyzeAllocationQuery, AllocationAnalysisDto>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInvestmentTypeRepository _typeRepository;

    public AnalyzeAllocationQueryHandler(
        IPortfolioRepository portfolioRepository,
        IInvestmentTypeRepository typeRepository)
    {
        _portfolioRepository = portfolioRepository;
        _typeRepository = typeRepository;
    }

    public async Task<AllocationAnalysisDto> Handle(AnalyzeAllocationQuery query, CancellationToken ct)
    {
        var portfolio = await _portfolioRepository.GetWithHoldingsAsync(query.Request.PortfolioId, ct)
            ?? throw new NotFoundException("Portfolio", query.Request.PortfolioId);
        if (portfolio.UserId != query.UserId)
            throw new UnauthorizedAccessException("The selected portfolio does not belong to the authenticated user.");

        var types = (await _typeRepository.GetAllAsync(ct)).ToDictionary(x => x.Id);
        var holdings = portfolio.Holdings.Where(x => x.IsActive && x.DeletedAt is null).ToList();
        var total = holdings.Sum(x => x.CurrentValue);
        var actual = holdings
            .GroupBy(x => types.TryGetValue(x.InvestmentTypeId, out var type) ? type.AssetClass.ToString() : "Other")
            .ToDictionary(x => x.Key, x => x.Sum(h => h.CurrentValue), StringComparer.OrdinalIgnoreCase);
        var targets = query.Request.Targets.ToDictionary(x => x.AssetClass, x => x.TargetPct, StringComparer.OrdinalIgnoreCase);

        var classes = actual.Keys.Union(targets.Keys, StringComparer.OrdinalIgnoreCase);
        var deviations = classes.Select(assetClass =>
        {
            var value = actual.GetValueOrDefault(assetClass);
            var actualPct = total == 0 ? 0 : Math.Round(value / total * 100, 2);
            var targetPct = targets.GetValueOrDefault(assetClass);
            var deviation = Math.Round(actualPct - targetPct, 2);
            var status = Math.Abs(deviation) < 2m ? "Balanced" : deviation > 0 ? "Overweight" : "Underweight";
            return new AllocationDeviationDto(assetClass, value, actualPct, targetPct, deviation, status);
        }).OrderByDescending(x => x.ActualPct).ToList();

        return new AllocationAnalysisDto(
            portfolio.Id, total, deviations, deviations.Any(x => Math.Abs(x.DeviationPct) >= 5m));
    }
}
