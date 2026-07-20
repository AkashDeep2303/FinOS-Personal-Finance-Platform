using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Enums;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Domain.Results;
using MediatR;

namespace FinOS.Loan.Application.Commands;

public class ExecutePrepaymentCommand : IRequest<LoanPrepaymentDto>
{
    public ExecutePrepaymentRequest Request { get; set; }

    public ExecutePrepaymentCommand(ExecutePrepaymentRequest request) { Request = request; }
}

public class ExecutePrepaymentCommandHandler : IRequestHandler<ExecutePrepaymentCommand, LoanPrepaymentDto>
{
    private readonly ILoanPrepaymentRepository _loanPrepaymentRepository;
    private readonly ILoanRepository _loanRepository;

    public ExecutePrepaymentCommandHandler(
        ILoanPrepaymentRepository loanPrepaymentRepository,
        ILoanRepository loanRepository)
    {
        _loanPrepaymentRepository = loanPrepaymentRepository;
        _loanRepository = loanRepository;
    }

    public async Task<LoanPrepaymentDto> Handle(ExecutePrepaymentCommand command, CancellationToken ct)
    {
        var req = command.Request;

        // Validate loan exists and allows prepayment
        var loan = await _loanRepository.GetWithPrepaymentsAsync(req.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), req.LoanId);

        if (!loan.IsPrepaymentAllowed)
            throw new DomainException("PREPAYMENT_NOT_ALLOWED", "Prepayment is not allowed for this loan.");

        // SP handles everything atomically: records prepayment, updates loan, regenerates schedule, debits account
        var result = await _loanPrepaymentRepository.ExecutePrepaymentAsync(
            req.LoanId,
            req.PrepaymentAmount,
            req.Strategy.ToString(),
            req.PrepaymentDate,
            req.Notes,
            ct);

        return new LoanPrepaymentDto
        {
            LoanId = req.LoanId,
            PrepaymentDate = req.PrepaymentDate,
            PrepaymentAmount = result.PrepaymentAmount,
            PenaltyAmount = result.PenaltyAmount,
            PrepaymentType = Enum.TryParse<PrepaymentType>(result.PrepaymentType, out var pt) ? pt : PrepaymentType.Partial,
            TenureReduction = result.PreviousTenureMonths - result.NewTenureMonths,
            InterestSaved = result.InterestSaved,
            NewOutstanding = result.NewOutstanding,
            NewEMI = result.NewEMI,
            NewTenureMonths = result.NewTenureMonths
        };
    }
}
