using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetMonthlyAggregatesQuery(long UserId, int Months = 12) : IRequest<List<MonthlyAggregateDto>>;

public class GetMonthlyAggregatesQueryHandler : IRequestHandler<GetMonthlyAggregatesQuery, List<MonthlyAggregateDto>>
{
    private readonly IMonthlyAggregateRepository _aggregateRepository;

    public GetMonthlyAggregatesQueryHandler(IMonthlyAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    public async Task<List<MonthlyAggregateDto>> Handle(GetMonthlyAggregatesQuery request, CancellationToken ct)
    {
        var aggregates = await _aggregateRepository.GetByUserAsync(request.UserId, request.Months, ct);

        return aggregates.Select(a => new MonthlyAggregateDto(
            a.Id, a.UserId, a.YearMonth, a.TotalIncome, a.TotalExpense,
            a.TotalSavings, a.SavingsRate, a.TopExpenseCategory, a.TopExpenseAmount,
            a.TransactionCount, a.CategoryBreakdown, a.CreatedAt
        )).OrderBy(a => a.YearMonth).ToList();
    }
}
