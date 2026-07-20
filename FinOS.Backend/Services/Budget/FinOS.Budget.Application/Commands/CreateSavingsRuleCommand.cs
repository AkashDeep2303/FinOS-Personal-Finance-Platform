using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Entities;
using FinOS.Budget.Domain.Enums;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Commands;

public class CreateSavingsRuleCommand : IRequest<SavingsRuleDto>
{
    public CreateSavingsRuleRequest Request { get; set; }

    public CreateSavingsRuleCommand(CreateSavingsRuleRequest request)
    {
        Request = request;
    }
}

public class CreateSavingsRuleCommandHandler : IRequestHandler<CreateSavingsRuleCommand, SavingsRuleDto>
{
    private readonly ISavingsRuleRepository _savingsRuleRepository;

    public CreateSavingsRuleCommandHandler(ISavingsRuleRepository savingsRuleRepository)
    {
        _savingsRuleRepository = savingsRuleRepository;
    }

    public async Task<SavingsRuleDto> Handle(CreateSavingsRuleCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var rule = new SavingsRule
        {
            UserId = req.UserId,
            RuleType = req.RuleType,
            Name = req.Name,
            TargetAccountId = req.TargetAccountId,
            SourceAccountId = req.SourceAccountId,
            RoundUpTo = req.RoundUpTo,
            Percentage = req.Percentage,
            FixedAmount = req.FixedAmount,
            Frequency = req.Frequency,
            DayOfMonth = req.DayOfMonth,
            IsActive = true,
            TotalSaved = 0
        };

        await _savingsRuleRepository.AddAsync(rule, ct);

        return new SavingsRuleDto
        {
            Id = rule.Id,
            UserId = rule.UserId,
            RuleType = rule.RuleType,
            RuleTypeDisplay = rule.RuleType.ToString(),
            Name = rule.Name,
            TargetAccountId = rule.TargetAccountId,
            SourceAccountId = rule.SourceAccountId,
            RoundUpTo = rule.RoundUpTo,
            Percentage = rule.Percentage,
            FixedAmount = rule.FixedAmount,
            Frequency = rule.Frequency,
            DayOfMonth = rule.DayOfMonth,
            IsActive = rule.IsActive,
            TotalSaved = rule.TotalSaved,
            CreatedAt = rule.CreatedAt
        };
    }
}
