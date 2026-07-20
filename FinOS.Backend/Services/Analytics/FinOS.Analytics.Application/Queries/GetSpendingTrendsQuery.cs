using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinOS.Analytics.Application.Queries;

public record GetSpendingTrendsQuery(long UserId, int Months = 6) : IRequest<List<SpendingTrendDto>>;

public class GetSpendingTrendsQueryHandler : IRequestHandler<GetSpendingTrendsQuery, List<SpendingTrendDto>>
{
    private readonly IMonthlyAggregateRepository _aggregateRepository;

    public GetSpendingTrendsQueryHandler(IMonthlyAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    public async Task<List<SpendingTrendDto>> Handle(GetSpendingTrendsQuery request, CancellationToken ct)
    {
        var aggregates = await _aggregateRepository.GetByUserAsync(request.UserId, request.Months, ct);
        var trends = new List<SpendingTrendDto>();

        foreach (var agg in aggregates)
        {
            if (string.IsNullOrEmpty(agg.CategoryBreakdown)) continue;

            try
            {
                var breakdown = JsonSerializer.Deserialize<List<CategoryBreakdownItem>>(agg.CategoryBreakdown);
                if (breakdown is null) continue;

                foreach (var item in breakdown)
                {
                    trends.Add(new SpendingTrendDto(agg.YearMonth, item.Category, item.Amount));
                }
            }
            catch (JsonException) { /* Skip malformed data */ }
        }

        return trends.OrderBy(t => t.YearMonth).ToList();
    }
}

internal record CategoryBreakdownItem(string Category, decimal Amount, decimal Percentage);
