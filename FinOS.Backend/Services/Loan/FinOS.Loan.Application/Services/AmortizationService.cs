using FinOS.Common.Helpers;
using FinOS.Loan.Domain.Entities;

namespace FinOS.Loan.Application.Services;

public interface IAmortizationService
{
    List<EMISchedule> GenerateSchedule(Domain.Entities.Loan loan);
}

public class AmortizationService : IAmortizationService
{
    public List<EMISchedule> GenerateSchedule(Domain.Entities.Loan loan)
    {
        var schedule = new List<EMISchedule>();
        var outstanding = loan.OutstandingPrincipal;
        var monthlyRate = loan.InterestRate / 12 / 100;
        var emi = loan.EMI;

        for (int i = 1; i <= loan.RemainingTenureMonths; i++)
        {
            var interestComponent = Math.Round(outstanding * monthlyRate, 2);
            var principalComponent = i == loan.RemainingTenureMonths
                ? outstanding // Last EMI adjustment
                : emi - interestComponent;

            // Adjust for last EMI
            var actualEmi = i == loan.RemainingTenureMonths
                ? outstanding + interestComponent
                : emi;

            var outstandingAfter = outstanding - principalComponent;
            if (outstandingAfter < 0) outstandingAfter = 0;

            var emiDate = CalculateEMIDate(loan.StartDate, i, loan.EMIDayOfMonth);

            schedule.Add(new EMISchedule
            {
                LoanId = loan.Id,
                EMINumber = i,
                EMIDate = emiDate,
                EMIAmount = Math.Round(actualEmi, 2),
                PrincipalComponent = Math.Round(principalComponent, 2),
                InterestComponent = Math.Round(interestComponent, 2),
                OutstandingBefore = Math.Round(outstanding, 2),
                OutstandingAfter = Math.Round(outstandingAfter, 2),
                IsPaid = false,
                LateFee = 0
            });

            outstanding = outstandingAfter;
        }

        return schedule;
    }

    private static DateTime CalculateEMIDate(DateTime startDate, int emiNumber, int emiDay)
    {
        var month = startDate.Month + emiNumber;
        var year = startDate.Year + (month - 1) / 12;
        month = ((month - 1) % 12) + 1;

        var day = Math.Min(emiDay, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day);
    }
}
