using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;
using FinOS.Budget.Application.Services;

namespace FinOS.Budget.Application.Commands;

public class DeleteBudgetCommand : IRequest<Unit>
{
    public long UserId { get; set; }
    public long BudgetId { get; set; }

    public DeleteBudgetCommand(long userId, long budgetId)
    {
        UserId = userId;
        BudgetId = budgetId;
    }
}

public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepository;

    public DeleteBudgetCommandHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<Unit> Handle(DeleteBudgetCommand command, CancellationToken ct)
    {
        var budget = await BudgetOwnership.GetOwnedAsync(
            _budgetRepository, command.BudgetId, command.UserId, ct);

        await _budgetRepository.SoftDeleteAsync(budget.Id, ct);

        return Unit.Value;
    }
}
