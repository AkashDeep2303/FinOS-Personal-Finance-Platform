using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class GetMonthlySummaryQuery : IRequest<MonthlySummaryDto>
{
    public long UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public class GetMonthlySummaryQueryHandler : IRequestHandler<GetMonthlySummaryQuery, MonthlySummaryDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetMonthlySummaryQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<MonthlySummaryDto> Handle(GetMonthlySummaryQuery query, CancellationToken ct)
    {
        var summary = await _transactionRepository.GetMonthlySummaryAsync(
            query.UserId, query.Year, query.Month, ct);

        return new MonthlySummaryDto
        {
            Year = query.Year,
            Month = query.Month,
            TotalIncome = summary.TotalIncome,
            TotalExpense = summary.TotalExpense,
            CategorySummaries = summary.CategorySummaries.Select(c => new CategorySummaryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                CategoryType = c.CategoryType,
                Amount = c.Amount,
                TransactionCount = c.TransactionCount,
                Percentage = c.Percentage
            }).ToList()
        };
    }
}
