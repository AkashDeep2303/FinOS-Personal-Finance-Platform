using FinOS.Common.Helpers;

namespace FinOS.Investment.Application.Services;

public interface IXIRRCalculator
{
    double Calculate(List<(DateTime Date, double Amount)> cashFlows, double guess = 0.1);
}

public class XIRRCalculator : IXIRRCalculator
{
    public double Calculate(List<(DateTime Date, double Amount)> cashFlows, double guess = 0.1)
    {
        return FinancialCalculator.CalculateXIRR(cashFlows, guess);
    }
}

public interface IEMICalculator
{
    decimal CalculateEMI(decimal principal, decimal annualRate, int tenureMonths);
    (decimal Principal, decimal Interest) SplitEMI(decimal emi, decimal outstandingPrincipal, decimal annualRate);
}

public class EMICalculator : IEMICalculator
{
    public decimal CalculateEMI(decimal principal, decimal annualRate, int tenureMonths)
    {
        return FinancialCalculator.CalculateEMI(principal, annualRate, tenureMonths);
    }

    public (decimal Principal, decimal Interest) SplitEMI(decimal emi, decimal outstandingPrincipal, decimal annualRate)
    {
        var monthlyRate = annualRate / 12 / 100;
        var interest = Math.Round(outstandingPrincipal * monthlyRate, 2);
        var principal = emi - interest;
        return (principal, interest);
    }
}
