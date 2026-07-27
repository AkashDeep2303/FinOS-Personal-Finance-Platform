using FinOS.Budget.Application.DTOs;
using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;
using FinOS.Budget.Application.Services;

namespace FinOS.Budget.Application.Commands;

public class UpdateSavingsRuleCommand : IRequest<SavingsRuleDto>
{
    public long UserId { get; set; }
    public long SavingsRuleId { get; set; }
    public UpdateSavingsRuleRequest Request { get; set; }

    public UpdateSavingsRuleCommand(long userId, long savingsRuleId, UpdateSavingsRuleRequest request)
    {
        UserId = userId;
        SavingsRuleId = savingsRuleId;
        Request = request;
    }
}

public class UpdateSavingsRuleCommandHandler : IRequestHandler<UpdateSavingsRuleCommand, SavingsRuleDto>
{
    private readonly ISavingsRuleRepository _savingsRuleRepository;

    public UpdateSavingsRuleCommandHandler(ISavingsRuleRepository savingsRuleRepository)
    {
        _savingsRuleRepository = savingsRuleRepository;
    }

    public async Task<SavingsRuleDto> Handle(UpdateSavingsRuleCommand command, CancellationToken ct)
    {
        var rule = await BudgetOwnership.GetOwnedRuleAsync(
            _savingsRuleRepository, command.SavingsRuleId, command.UserId, ct);

        var req = command.Request;
        if (req.Name is not null) rule.Name = req.Name;
        if (req.TargetAccountId.HasValue) rule.TargetAccountId = req.TargetAccountId;
        if (req.SourceAccountId.HasValue) rule.SourceAccountId = req.SourceAccountId;
        if (req.RoundUpTo.HasValue) rule.RoundUpTo = req.RoundUpTo;
        if (req.Percentage.HasValue) rule.Percentage = req.Percentage;
        if (req.FixedAmount.HasValue) rule.FixedAmount = req.FixedAmount;
        if (req.Frequency.HasValue) rule.Frequency = req.Frequency.Value;
        if (req.DayOfMonth.HasValue) rule.DayOfMonth = req.DayOfMonth;
        if (req.IsActive.HasValue) rule.IsActive = req.IsActive.Value;

        await _savingsRuleRepository.UpdateAsync(rule, ct);

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
