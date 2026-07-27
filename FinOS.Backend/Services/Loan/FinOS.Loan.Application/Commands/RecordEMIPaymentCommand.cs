using FinOS.Common.Exceptions;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Interfaces;
using FinOS.Loan.Domain.Results;
using MediatR;
using FinOS.Loan.Application.Services;

namespace FinOS.Loan.Application.Commands;

public class RecordEMIPaymentCommand : IRequest<EMIScheduleDto>
{
    public long UserId { get; set; }
    public RecordEMIPaymentRequest Request { get; set; }

    public RecordEMIPaymentCommand(long userId, RecordEMIPaymentRequest request) { UserId = userId; Request = request; }
}

public class RecordEMIPaymentCommandHandler : IRequestHandler<RecordEMIPaymentCommand, EMIScheduleDto>
{
    private readonly IEMIScheduleRepository _emiScheduleRepository;
    private readonly ILoanRepository _loanRepository;

    public RecordEMIPaymentCommandHandler(
        IEMIScheduleRepository emiScheduleRepository,
        ILoanRepository loanRepository)
    {
        _emiScheduleRepository = emiScheduleRepository;
        _loanRepository = loanRepository;
    }

    public async Task<EMIScheduleDto> Handle(RecordEMIPaymentCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await LoanOwnership.GetOwnedAsync(
            _loanRepository, req.LoanId, command.UserId, ct);

        // SP handles everything atomically: marks EMI paid, updates loan totals, debits account
        var result = await _emiScheduleRepository.RecordEMIPaymentAsync(
            req.LoanId,
            req.EMINumber,
            req.PaidDate,
            req.PaidAmount,
            req.LateFee,
            ct);

        // Fetch the updated EMI to build the response DTO
        var loan = await _loanRepository.GetWithScheduleAsync(req.LoanId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Loan), req.LoanId);

        var emi = loan.EMISchedule.FirstOrDefault(e => e.EMINumber == req.EMINumber)
            ?? throw new NotFoundException(nameof(Domain.Entities.EMISchedule), req.EMINumber);

        return new EMIScheduleDto
        {
            Id = emi.Id, LoanId = emi.LoanId, EMINumber = emi.EMINumber,
            EMIDate = emi.EMIDate, EMIAmount = emi.EMIAmount,
            PrincipalComponent = emi.PrincipalComponent, InterestComponent = emi.InterestComponent,
            OutstandingBefore = emi.OutstandingBefore, OutstandingAfter = emi.OutstandingAfter,
            IsPaid = emi.IsPaid, PaidDate = emi.PaidDate, PaidAmount = emi.PaidAmount, LateFee = emi.LateFee
        };
    }
}
