using FinOS.Common.Exceptions;
using FinOS.Goals.Domain.Interfaces;
using MediatR;
using FinOS.Goals.Application.Services;

namespace FinOS.Goals.Application.Commands;

public record DeleteGoalCommand(long UserId, long GoalId) : IRequest<Unit>;

public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand, Unit>
{
    private readonly IGoalRepository _goalRepository;

    public DeleteGoalCommandHandler(IGoalRepository goalRepository) => _goalRepository = goalRepository;

    public async Task<Unit> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await GoalOwnership.GetOwnedAsync(
            _goalRepository, request.GoalId, request.UserId, cancellationToken);
        await _goalRepository.SoftDeleteAsync(goal.Id, cancellationToken);
        return Unit.Value;
    }
}
