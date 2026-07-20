using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Commands;

public class GenerateAmortizationScheduleCommand : IRequest<List<EMIScheduleDto>>
{
    public long LoanId { get; set; }

    public GenerateAmortizationScheduleCommand(long loanId) { LoanId = loanId; }
}

public class GenerateAmortizationScheduleCommandHandler : IRequestHandler<GenerateAmortizationScheduleCommand, List<EMIScheduleDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GenerateAmortizationScheduleCommandHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<List<EMIScheduleDto>> Handle(GenerateAmortizationScheduleCommand command, CancellationToken ct)
    {
        // Validate loan exists
        var loan = await _loanRepository.GetByIdAsync(command.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), command.LoanId);

        // SP deletes existing unpaid EMIs and regenerates the schedule
        await _loanRepository.GenerateAmortizationScheduleAsync(command.LoanId, ct);

        // Re-fetch to get the newly generated schedule
        var updatedLoan = await _loanRepository.GetWithScheduleAsync(command.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), command.LoanId);

        return updatedLoan.EMISchedule.Select(e => new EMIScheduleDto
        {
            Id = e.Id, LoanId = e.LoanId, EMINumber = e.EMINumber,
            EMIDate = e.EMIDate, EMIAmount = e.EMIAmount,
            PrincipalComponent = e.PrincipalComponent, InterestComponent = e.InterestComponent,
            OutstandingBefore = e.OutstandingBefore, OutstandingAfter = e.OutstandingAfter,
            IsPaid = e.IsPaid, PaidDate = e.PaidDate, PaidAmount = e.PaidAmount, LateFee = e.LateFee
        }).ToList();
    }
}
