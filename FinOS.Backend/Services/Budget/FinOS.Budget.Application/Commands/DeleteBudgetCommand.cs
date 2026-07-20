using FinOS.Budget.Domain.Interfaces;
using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using MediatR;

namespace FinOS.Budget.Application.Commands;

public class DeleteBudgetCommand : IRequest<Unit>
{
    public long BudgetId { get; set; }

    public DeleteBudgetCommand(long budgetId)
    {
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
        var budget = await _budgetRepository.GetByIdAsync(command.BudgetId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Budget), command.BudgetId);

        await _budgetRepository.SoftDeleteAsync(budget.Id, ct);

        return Unit.Value;
    }
}
