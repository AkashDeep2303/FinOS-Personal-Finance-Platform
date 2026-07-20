using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetIncomeVsExpenseTrendQuery(long UserId, int Months = 12) : IRequest<List<IncomeVsExpenseDto>>;

public class GetIncomeVsExpenseTrendQueryHandler : IRequestHandler<GetIncomeVsExpenseTrendQuery, List<IncomeVsExpenseDto>>
{
    private readonly IMonthlyAggregateRepository _aggregateRepository;

    public GetIncomeVsExpenseTrendQueryHandler(IMonthlyAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    public async Task<List<IncomeVsExpenseDto>> Handle(GetIncomeVsExpenseTrendQuery request, CancellationToken ct)
    {
        var aggregates = await _aggregateRepository.GetByUserAsync(request.UserId, request.Months, ct);

        return aggregates.Select(a => new IncomeVsExpenseDto(
            a.YearMonth, a.TotalIncome, a.TotalExpense, a.TotalSavings
        )).OrderBy(a => a.YearMonth).ToList();
    }
}
