using FinOS.Common.Helpers;
using FinOS.Loan.Application.DTOs;
using FinOS.Loan.Domain.Entities;
using FinOS.Loan.Domain.Enums;
using FinOS.Loan.Domain.Interfaces;
using MediatR;

namespace FinOS.Loan.Application.Commands;

public class CreateLoanCommand : IRequest<LoanDto>
{
    public CreateLoanRequest Request { get; set; }

    public CreateLoanCommand(CreateLoanRequest request)
    {
        Request = request;
    }
}

public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;

    public CreateLoanCommandHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<LoanDto> Handle(CreateLoanCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var emi = FinancialCalculator.CalculateEMI(req.PrincipalAmount, req.InterestRate, req.TenureMonths);
        var totalAmountPayable = emi * req.TenureMonths;
        var totalInterest = totalAmountPayable - req.PrincipalAmount;
        var maturityDate = req.StartDate.AddMonths(req.TenureMonths);

        var loan = new Domain.Entities.Loan
        {
            UserId = req.UserId,
            LoanTypeId = req.LoanTypeId,
            AccountId = req.AccountId,
            LenderName = req.LenderName,
            LoanAccountNumber = req.LoanAccountNumber,
            PrincipalAmount = req.PrincipalAmount,
            OutstandingPrincipal = req.PrincipalAmount,
            InterestRate = req.InterestRate,
            InterestType = req.InterestType,
            TenureMonths = req.TenureMonths,
            RemainingTenureMonths = req.TenureMonths,
            EMI = emi,
            EMIDayOfMonth = req.EMIDayOfMonth,
            StartDate = req.StartDate,
            MaturityDate = maturityDate,
            DisbursementDate = req.DisbursementDate ?? req.StartDate,
            ProcessingFee = req.ProcessingFee,
            PrepaymentPenaltyPct = req.PrepaymentPenaltyPct,
            IsPrepaymentAllowed = req.IsPrepaymentAllowed,
            TotalInterestPayable = totalInterest,
            TotalAmountPayable = totalAmountPayable,
            TotalPaid = 0,
            TotalInterestPaid = 0,
            TotalPrepaid = 0,
            NextEMIDate = CalculateNextEMIDate(req.StartDate, req.EMIDayOfMonth),
            Status = LoanStatus.Active,
            Currency = req.Currency,
            Notes = req.Notes
        };

        // SP creates the loan and returns the new ID
        var loanId = await _loanRepository.CreateAsync(loan, ct);

        // SP generates the full amortization schedule
        await _loanRepository.GenerateAmortizationScheduleAsync(loanId, ct);

        // Re-fetch the loan to populate the DTO with SP-computed values
        var createdLoan = await _loanRepository.GetWithScheduleAsync(loanId, ct)
            ?? throw new InvalidOperationException($"Loan {loanId} not found after creation.");

        return MapToDto(createdLoan);
    }

    private static DateTime CalculateNextEMIDate(DateTime startDate, int emiDay)
    {
        var next = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
        var day = Math.Min(emiDay, DateTime.DaysInMonth(next.Year, next.Month));
        return new DateTime(next.Year, next.Month, day);
    }

    private static LoanDto MapToDto(Domain.Entities.Loan loan) => new()
    {
        Id = loan.Id, UserId = loan.UserId, LoanTypeId = loan.LoanTypeId,
        LenderName = loan.LenderName, LoanAccountNumber = loan.LoanAccountNumber,
        PrincipalAmount = loan.PrincipalAmount, OutstandingPrincipal = loan.OutstandingPrincipal,
        InterestRate = loan.InterestRate, InterestType = loan.InterestType,
        TenureMonths = loan.TenureMonths, RemainingTenureMonths = loan.RemainingTenureMonths,
        EMI = loan.EMI, EMIDayOfMonth = loan.EMIDayOfMonth,
        StartDate = loan.StartDate, MaturityDate = loan.MaturityDate,
        Status = loan.Status, Currency = loan.Currency,
        TotalInterestPayable = loan.TotalInterestPayable,
        TotalAmountPayable = loan.TotalAmountPayable,
        TotalPaid = loan.TotalPaid, TotalInterestPaid = loan.TotalInterestPaid,
        NextEMIDate = loan.NextEMIDate
    };
}
