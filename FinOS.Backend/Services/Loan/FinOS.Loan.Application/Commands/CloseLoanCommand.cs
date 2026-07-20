using FinOS.Common.Exceptions;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Commands;

public class CloseLoanCommand : IRequest<Unit>
{
    public long LoanId { get; set; }

    public CloseLoanCommand(long loanId) { LoanId = loanId; }
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
        var loan = await _loanRepository.GetByIdAsync(command.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), command.LoanId);

        // SP sets Status = Closed, OutstandingPrincipal = 0, RemainingTenureMonths = 0, NextEMIDate = NULL
        await _loanRepository.CloseLoanAsync(command.LoanId, ct);

        return Unit.Value;
    }
}
