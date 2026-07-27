using FinOS.Common.Exceptions;
using FinOS.Loan.Domain.Interfaces;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Commands;

public class CloseLoanCommand : IRequest<Unit>
{
    public long UserId { get; set; }
    public long LoanId { get; set; }

    public CloseLoanCommand(long userId, long loanId) { UserId = userId; LoanId = loanId; }
}

public class CloseLoanCommandHandler : IRequestHandler<CloseLoanCommand, Unit>
{
    private readonly ILoanRepository _loanRepository;

    public CloseLoanCommandHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<Unit> Handle(CloseLoanCommand command, CancellationToken ct)
    {
        // Validate loan exists
        await LoanOwnership.GetOwnedAsync(
            _loanRepository, command.LoanId, command.UserId, ct);

        // SP sets Status = Closed, OutstandingPrincipal = 0, RemainingTenureMonths = 0, NextEMIDate = NULL
        await _loanRepository.CloseLoanAsync(command.LoanId, ct);

        return Unit.Value;
    }
}
