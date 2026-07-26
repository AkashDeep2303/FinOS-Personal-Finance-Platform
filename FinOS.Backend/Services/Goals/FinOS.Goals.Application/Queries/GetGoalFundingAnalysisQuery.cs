using FinOS.Common.Helpers;
using FinOS.Goals.Application.DTOs;
using FinOS.Goals.Domain.Enums;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Queries;

public record GetGoalFundingAnalysisQuery(long UserId, decimal AvailableMonthlySurplus)
    : IRequest<GoalFundingAnalysisDto>;

public class GetGoalFundingAnalysisQueryHandler
    : IRequestHandler<GetGoalFundingAnalysisQuery, GoalFundingAnalysisDto>
{
    private readonly IGoalRepository _goalRepository;
    public GetGoalFundingAnalysisQueryHandler(IGoalRepository goalRepository) => _goalRepository = goalRepository;

    public async Task<GoalFundingAnalysisDto> Handle(GetGoalFundingAnalysisQuery query, CancellationToken ct)
    {
        var goals = await _goalRepository.GetByUserIdAsync(query.UserId, ct);
        var today = DateTime.UtcNow.Date;
        var items = goals
            .Where(g => g.Status == GoalStatus.Active)
            .Select(g =>
            {
                var remaining = Math.Max(0, g.TargetAmount - g.CurrentAmount);
                var monthsToTarget = g.TargetDate.HasValue
                    ? Math.Max(1, (g.TargetDate.Value.Year - today.Year) * 12 + g.TargetDate.Value.Month - today.Month)
                    : 0;
                var required = remaining == 0 ? 0 :
                    monthsToTarget > 0 ? FinancialCalculator.RequiredMonthlyContribution(g.TargetAmount, g.CurrentAmount, 0, monthsToTarget) : 0;
                var completionMonths = g.MonthlyContribution > 0
                    ? (int)Math.Ceiling(remaining / g.MonthlyContribution) : (int?)null;
                var projected = completionMonths.HasValue ? today.AddMonths(completionMonths.Value) : g.ProjectedDate;
                var variance = projected.HasValue && g.TargetDate.HasValue
                    ? (projected.Value.Year - g.TargetDate.Value.Year) * 12 + projected.Value.Month - g.TargetDate.Value.Month
                    : (int?)null;
                var status = remaining == 0 ? "Completed" :
                    !g.TargetDate.HasValue ? "No Target Date" :
                    !projected.HasValue ? "Unfunded" :
                    variance <= 0 ? (variance < 0 ? "Ahead" : "On Track") : "Behind";

                return new GoalFundingItemDto(
                    g.Id, g.Name, g.Category, g.Priority.ToString(), g.TargetAmount,
                    g.CurrentAmount, remaining, g.TargetDate, required, g.MonthlyContribution,
                    projected, variance, status);
            })
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.TargetDate)
            .ToList();

        var totalRequired = items.Sum(x => x.RequiredMonthlyContribution);
        var deficit = Math.Max(0, totalRequired - query.AvailableMonthlySurplus);
        return new GoalFundingAnalysisDto(
            query.AvailableMonthlySurplus, totalRequired, deficit, deficit > 0, items);
    }
}
