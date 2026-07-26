using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Application.Services;
using FinOS.Budget.Domain.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Queries;

public sealed record GetSavingsRuleQuery(long UserId, long RuleId) : IRequest<SavingsRuleDto>;

public sealed class GetSavingsRuleQueryHandler(ISavingsRuleRepository repository)
    : IRequestHandler<GetSavingsRuleQuery, SavingsRuleDto>
{
    public async Task<SavingsRuleDto> Handle(GetSavingsRuleQuery request, CancellationToken cancellationToken)
    {
        var rule = await BudgetOwnership.GetOwnedRuleAsync(
            repository, request.RuleId, request.UserId, cancellationToken);
        return new SavingsRuleDto
        {
            Id = rule.Id, UserId = rule.UserId, RuleType = rule.RuleType,
            RuleTypeDisplay = rule.RuleType.ToString(), Name = rule.Name,
            TargetAccountId = rule.TargetAccountId, SourceAccountId = rule.SourceAccountId,
            RoundUpTo = rule.RoundUpTo, Percentage = rule.Percentage,
            FixedAmount = rule.FixedAmount, Frequency = rule.Frequency,
            DayOfMonth = rule.DayOfMonth, IsActive = rule.IsActive,
            TotalSaved = rule.TotalSaved, CreatedAt = rule.CreatedAt
        };
    }
}
