using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Enums;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Domain.Results;
using MediatR;

namespace FinOS.Loan.Application.Commands;

public class SimulatePrepaymentCommand : IRequest<PrepaymentSimulationDto>
{
    public SimulatePrepaymentRequest Request { get; set; }

    public SimulatePrepaymentCommand(SimulatePrepaymentRequest request) { Request = request; }
}

public class SimulatePrepaymentCommandHandler : IRequestHandler<SimulatePrepaymentCommand, PrepaymentSimulationDto>
{
    private readonly ILoanPrepaymentRepository _loanPrepaymentRepository;
    private readonly ILoanRepository _loanRepository;

    public SimulatePrepaymentCommandHandler(
        ILoanPrepaymentRepository loanPrepaymentRepository,
        ILoanRepository loanRepository)
    {
        _loanPrepaymentRepository = loanPrepaymentRepository;
        _loanRepository = loanRepository;
    }

    public async Task<PrepaymentSimulationDto> Handle(SimulatePrepaymentCommand command, CancellationToken ct)
    {
        var req = command.Request;

        // Validate loan exists and allows prepayment
        var loan = await _loanRepository.GetWithScheduleAsync(req.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), req.LoanId);

        if (!loan.IsPrepaymentAllowed)
            throw new DomainException("PREPAYMENT_NOT_ALLOWED", "Prepayment is not allowed for this loan.");

        // SP calculates what-if scenario without persisting changes
        var result = await _loanPrepaymentRepository.SimulatePrepaymentAsync(
            req.LoanId,
            req.PrepaymentAmount,
            req.Strategy.ToString(),
            req.PrepaymentDate,
            ct);

        return new PrepaymentSimulationDto
        {
            SimulationName = req.SimulationName ?? $"{req.Strategy} simulation",
            PrepaymentAmount = result.PrepaymentAmount,
            Strategy = req.Strategy,
            StrategyDisplay = result.Strategy,
            OriginalTenureMonths = result.OriginalTenureMonths,
            NewTenureMonths = result.NewTenureMonths,
            TenureSaved = result.TenureSaved,
            OriginalTotalInterest = result.OriginalTotalInterest,
            NewTotalInterest = result.NewTotalInterest,
            InterestSaved = result.InterestSaved,
            OriginalEMI = result.OriginalEMI,
            NewEMI = result.NewEMI,
            PenaltyEstimate = result.PenaltyEstimate
        };
    }
}
