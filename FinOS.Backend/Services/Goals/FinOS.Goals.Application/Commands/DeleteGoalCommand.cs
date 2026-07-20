using FinOS.Common.Exceptions;
using FinOS.Goals.Domain.Interfaces;
using MediatR;

namespace FinOS.Goals.Application.Commands;

public record DeleteGoalCommand(long GoalId) : IRequest<Unit>;

public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand, Unit>
{
    private readonly IGoalRepository _goalRepository;

    public DeleteGoalCommandHandler(IGoalRepository goalRepository) => _goalRepository = goalRepository;

    public async Task<Unit> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(request.GoalId, cancellationToken)
            ?? throw new NotFoundException("Goal", request.GoalId);
        await _goalRepository.SoftDeleteAsync(goal.Id, cancellationToken);
        return Unit.Value;
    }
}