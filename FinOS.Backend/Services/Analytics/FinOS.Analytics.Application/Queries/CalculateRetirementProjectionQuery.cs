using FinOS.Analytics.Application.DTOs;
using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.Analytics.Application.Queries;

public record CalculateRetirementProjectionQuery(RetirementProjectionRequest Request) : IRequest<RetirementProjectionDto>;

public class CalculateRetirementProjectionQueryHandler
    : IRequestHandler<CalculateRetirementProjectionQuery, RetirementProjectionDto>
{
    public Task<RetirementProjectionDto> Handle(CalculateRetirementProjectionQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var yearsToRetirement = request.RetirementAge - request.CurrentAge;
        var retirementYears = request.LifeExpectancy - request.RetirementAge;
        var monthsToRetirement = yearsToRetirement * 12;
        var baseExpense = request.DesiredRetirementExpense > 0
            ? request.DesiredRetirementExpense
            : request.CurrentMonthlyExpense;

        var firstExpense = FinancialCalculator.InflationAdjustedValue(
            baseExpense, request.AnnualInflationRate, yearsToRetirement);
        var target = FinancialCalculator.RetirementCorpus(
            firstExpense, request.AnnualPostRetirementReturn,
            request.AnnualInflationRate, retirementYears * 12);
        var projected = FinancialCalculator.FutureValueWithMonthlyContributions(
            request.CurrentRetirementCorpus, request.MonthlyRetirementContribution,
            request.AnnualPreRetirementReturn, monthsToRetirement);
        var gap = Math.Max(0, target - projected);
        var required = FinancialCalculator.RequiredMonthlyContribution(
            target, request.CurrentRetirementCorpus,
            request.AnnualPreRetirementReturn, monthsToRetirement);
        var readiness = target == 0 ? 100 : (int)Math.Clamp(Math.Round(projected / target * 100), 0, 100);
        var status = readiness >= 100 ? "On Track" : readiness >= 75 ? "Close" : readiness >= 50 ? "Needs Attention" : "Funding Gap";

        return Task.FromResult(new RetirementProjectionDto(
            yearsToRetirement, retirementYears, firstExpense, target, projected, gap,
            required, readiness, status,
            new[]
            {
                "Returns and inflation are user-provided assumptions, not guaranteed outcomes.",
                "Contributions are assumed at month end.",
                "The projection excludes tax unless reflected in the entered return assumptions.",
                "Retirement spending is assumed to rise monthly with inflation through life expectancy."
            }));
    }
}
