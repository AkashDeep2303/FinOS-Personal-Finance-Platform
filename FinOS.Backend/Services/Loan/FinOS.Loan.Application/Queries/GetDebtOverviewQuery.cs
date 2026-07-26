using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Queries;

public record GetDebtOverviewQuery(long UserId) : IRequest<DebtOverviewDto>;

public class GetDebtOverviewQueryHandler : IRequestHandler<GetDebtOverviewQuery, DebtOverviewDto>
{
    private readonly ILoanRepository _repository;
    public GetDebtOverviewQueryHandler(ILoanRepository repository) => _repository = repository;

    public async Task<DebtOverviewDto> Handle(GetDebtOverviewQuery query, CancellationToken ct)
    {
        var result = await _repository.GetDebtOverviewAsync(query.UserId, ct);
        return new DebtOverviewDto(
            result.TotalOutstandingDebt, result.TotalMonthlyEMI, result.ActiveLoanCount,
            result.MonthlyIncome, result.DebtToIncomeRatioPct, result.RiskCategory,
            result.MonthlySurplusAfterEMI, result.WeightedInterestRate, result.DebtFreeDate);
    }
}
