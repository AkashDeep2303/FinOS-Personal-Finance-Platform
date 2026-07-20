using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinOS.Analytics.Application.Queries;

public record GetCategoryWiseBreakdownQuery(long UserId, int YearMonth) : IRequest<List<CategoryBreakdownDto>>;

public class GetCategoryWiseBreakdownQueryHandler : IRequestHandler<GetCategoryWiseBreakdownQuery, List<CategoryBreakdownDto>>
{
    private readonly IMonthlyAggregateRepository _aggregateRepository;

    public GetCategoryWiseBreakdownQueryHandler(IMonthlyAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    public async Task<List<CategoryBreakdownDto>> Handle(GetCategoryWiseBreakdownQuery request, CancellationToken ct)
    {
        var aggregate = await _aggregateRepository.GetByUserAndMonthAsync(request.UserId, request.YearMonth, ct);

        if (aggregate?.CategoryBreakdown is null) return new List<CategoryBreakdownDto>();

        try
        {
            var items = JsonSerializer.Deserialize<List<CategoryBreakdownJsonItem>>(aggregate.CategoryBreakdown);
            var total = items?.Sum(i => i.Total) ?? 0;
            return items?.Select(i => new CategoryBreakdownDto(i.Category, i.Total, total == 0 ? 0 : Math.Round(i.Total * 100 / total, 2))).ToList()
                ?? new List<CategoryBreakdownDto>();
        }
        catch (JsonException)
        {
            return new List<CategoryBreakdownDto>();
        }
    }
}

internal record CategoryBreakdownJsonItem(string Category, decimal Total);
