using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Queries;

public class GetSavingsRulesQuery : IRequest<List<SavingsRuleDto>>
{
    public long UserId { get; set; }
    public bool? IsActive { get; set; }

    public GetSavingsRulesQuery(long userId, bool? isActive = null)
    {
        UserId = userId;
        IsActive = isActive;
    }
}

public class GetSavingsRulesQueryHandler : IRequestHandler<GetSavingsRulesQuery, List<SavingsRuleDto>>
{
    private readonly ISavingsRuleRepository _savingsRuleRepository;

    public GetSavingsRulesQueryHandler(ISavingsRuleRepository savingsRuleRepository)
    {
        _savingsRuleRepository = savingsRuleRepository;
    }

    public async Task<List<SavingsRuleDto>> Handle(GetSavingsRulesQuery query, CancellationToken ct)
    {
        var rules = query.IsActive == true
            ? await _savingsRuleRepository.GetActiveByUserIdAsync(query.UserId, ct)
            : await _savingsRuleRepository.GetByUserIdAsync(query.UserId, ct);

        return rules.Select(r => new SavingsRuleDto
        {
            Id = r.Id, UserId = r.UserId, RuleType = r.RuleType,
            RuleTypeDisplay = r.RuleType.ToString(), Name = r.Name,
            TargetAccountId = r.TargetAccountId, SourceAccountId = r.SourceAccountId,
            RoundUpTo = r.RoundUpTo, Percentage = r.Percentage,
            FixedAmount = r.FixedAmount, Frequency = r.Frequency,
            DayOfMonth = r.DayOfMonth, IsActive = r.IsActive,
            TotalSaved = r.TotalSaved, CreatedAt = r.CreatedAt
        }).ToList();
    }
}
