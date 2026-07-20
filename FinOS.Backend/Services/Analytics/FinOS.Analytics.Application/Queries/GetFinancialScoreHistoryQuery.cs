using FinOS.Analytics.Application.DTOs;
using FinOS.Analytics.Domain.Interfaces;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record GetFinancialScoreHistoryQuery(long UserId, int Months = 12) : IRequest<List<FinancialScoreDto>>;

public class GetFinancialScoreHistoryQueryHandler : IRequestHandler<GetFinancialScoreHistoryQuery, List<FinancialScoreDto>>
{
    private readonly IFinancialScoreRepository _scoreRepository;

    public GetFinancialScoreHistoryQueryHandler(IFinancialScoreRepository scoreRepository)
    {
        _scoreRepository = scoreRepository;
    }

    public async Task<List<FinancialScoreDto>> Handle(GetFinancialScoreHistoryQuery request, CancellationToken ct)
    {
        var scores = await _scoreRepository.GetHistoryByUserAsync(request.UserId, request.Months, ct);

        return scores.Select(s => new FinancialScoreDto(
            s.Id, s.UserId, s.ScoreDate, s.OverallScore, s.ScoreGrade,
            s.SavingsRateScore, s.DebtToIncomeScore, s.EmergencyFundScore,
            s.InvestmentScore, s.GoalProgressScore, s.SavingsRatePct,
            s.DebtToIncomeRatio, s.EmergencyFundMonths, s.InvestmentToIncomeRatio,
            s.MonthlyIncome, s.MonthlyExpenses, s.MonthlySavings,
            s.TotalDebt, s.TotalInvestments, s.Recommendations, s.CreatedAt
        )).OrderBy(s => s.ScoreDate).ToList();
    }
}
