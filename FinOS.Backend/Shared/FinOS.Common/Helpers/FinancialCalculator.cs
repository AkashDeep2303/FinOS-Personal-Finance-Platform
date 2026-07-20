namespace FinOS.Common.Helpers;

/// <summary>
/// Core financial calculation utilities used across FinOS modules.
/// All monetary calculations use <c>decimal</c> for precision.
/// Rate inputs are expressed as absolute decimals (e.g. 0.05 for 5%).
/// </summary>
public static class FinancialCalculator
{
    // ── Simple Interest ──────────────────────────────────────────────────

    /// <summary>
    /// Calculates simple interest: I = P × R × T
    /// </summary>
    /// <param name="principal">Principal amount.</param>
    /// <param name="annualRate">Annual interest rate as a decimal (e.g. 0.05 for 5%).</param>
    /// <param name="timeInYears">Time period in years.</param>
    /// <returns>Interest amount.</returns>
    public static decimal SimpleInterest(decimal principal, decimal annualRate, decimal timeInYears)
    {
        if (principal < 0) throw new ArgumentException("Principal cannot be negative.", nameof(principal));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (timeInYears < 0) throw new ArgumentException("Time cannot be negative.", nameof(timeInYears));

        return principal * annualRate * timeInYears;
    }

    /// <summary>
    /// Calculates the future value using simple interest: FV = P × (1 + R × T)
    /// </summary>
    public static decimal SimpleInterestFutureValue(decimal principal, decimal annualRate, decimal timeInYears)
    {
        return principal * (1 + annualRate * timeInYears);
    }

    // ── Compound Interest ────────────────────────────────────────────────

    /// <summary>
    /// Calculates compound interest future value: FV = P × (1 + R/N)^(N×T)
    /// </summary>
    /// <param name="principal">Principal amount.</param>
    /// <param name="annualRate">Annual interest rate as a decimal.</param>
    /// <param name="timeInYears">Time period in years.</param>
    /// <param name="compoundingPeriodsPerYear">Number of compounding periods per year (1=annual, 12=monthly, 365=daily).</param>
    /// <returns>Future value.</returns>
    public static decimal CompoundInterestFutureValue(
        decimal principal,
        decimal annualRate,
        decimal timeInYears,
        int compoundingPeriodsPerYear = 12)
    {
        if (principal < 0) throw new ArgumentException("Principal cannot be negative.", nameof(principal));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (timeInYears < 0) throw new ArgumentException("Time cannot be negative.", nameof(timeInYears));
        if (compoundingPeriodsPerYear <= 0) throw new ArgumentException("Compounding periods must be positive.", nameof(compoundingPeriodsPerYear));

        decimal ratePerPeriod = annualRate / compoundingPeriodsPerYear;
        int totalPeriods = (int)(compoundingPeriodsPerYear * timeInYears);

        decimal factor = 1m;
        for (int i = 0; i < totalPeriods; i++)
        {
            factor *= (1 + ratePerPeriod);
        }

        return principal * factor;
    }

    /// <summary>
    /// Calculates compound interest earned: CI = FV − P
    /// </summary>
    public static decimal CompoundInterest(
        decimal principal,
        decimal annualRate,
        decimal timeInYears,
        int compoundingPeriodsPerYear = 12)
    {
        return CompoundInterestFutureValue(principal, annualRate, timeInYears, compoundingPeriodsPerYear) - principal;
    }

    // ── Effective Annual Rate ────────────────────────────────────────────

    /// <summary>
    /// Converts a nominal annual rate to an effective annual rate.
    /// EAR = (1 + R/N)^N − 1
    /// </summary>
    public static decimal EffectiveAnnualRate(decimal nominalAnnualRate, int compoundingPeriodsPerYear)
    {
        if (nominalAnnualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(nominalAnnualRate));
        if (compoundingPeriodsPerYear <= 0) throw new ArgumentException("Compounding periods must be positive.", nameof(compoundingPeriodsPerYear));

        decimal ratePerPeriod = nominalAnnualRate / compoundingPeriodsPerYear;
        decimal factor = 1m;
        for (int i = 0; i < compoundingPeriodsPerYear; i++)
        {
            factor *= (1 + ratePerPeriod);
        }
        return factor - 1;
    }

    // ── Amortization / EMI ───────────────────────────────────────────────

    /// <summary>
    /// Calculates the Equated Monthly Installment (EMI) for a loan.
    /// EMI = P × R × (1+R)^N / ((1+R)^N − 1)
    /// </summary>
    /// <param name="principal">Loan principal amount.</param>
    /// <param name="annualRate">Annual interest rate as a decimal.</param>
    /// <param name="termInMonths">Loan term in months.</param>
    /// <returns>Monthly installment amount.</returns>
    public static decimal CalculateEMI(decimal principal, decimal annualRate, int termInMonths)
    {
        if (principal <= 0) throw new ArgumentException("Principal must be positive.", nameof(principal));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (termInMonths <= 0) throw new ArgumentException("Term must be positive.", nameof(termInMonths));

        // Zero interest edge case
        if (annualRate == 0)
        {
            return principal / termInMonths;
        }

        decimal monthlyRate = annualRate / 12;

        decimal factor = 1m;
        for (int i = 0; i < termInMonths; i++)
        {
            factor *= (1 + monthlyRate);
        }

        return principal * monthlyRate * factor / (factor - 1);
    }

    /// <summary>
    /// Calculates the total interest paid over the life of a loan.
    /// </summary>
    public static decimal TotalInterest(decimal principal, decimal annualRate, int termInMonths)
    {
        decimal emi = CalculateEMI(principal, annualRate, termInMonths);
        return (emi * termInMonths) - principal;
    }

    // ── Present Value / Discounting ──────────────────────────────────────

    /// <summary>
    /// Calculates the present value of a future amount: PV = FV / (1 + R)^T
    /// </summary>
    public static decimal PresentValue(decimal futureValue, decimal annualRate, decimal timeInYears)
    {
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));

        decimal discountFactor = 1m;
        for (int i = 0; i < timeInYears; i++)
        {
            discountFactor *= (1 + annualRate);
        }
        return futureValue / discountFactor;
    }

    /// <summary>
    /// Calculates the Net Present Value (NPV) of a series of cash flows.
    /// </summary>
    /// <param name="cashFlows">Cash flows where index 0 is the initial investment (typically negative).</param>
    /// <param name="discountRate">Discount rate per period.</param>
    /// <returns>Net Present Value.</returns>
    public static decimal NetPresentValue(IEnumerable<decimal> cashFlows, decimal discountRate)
    {
        if (cashFlows is null) throw new ArgumentNullException(nameof(cashFlows));

        decimal npv = 0m;
        decimal discountFactor = 1m;

        int period = 0;
        foreach (var cashFlow in cashFlows)
        {
            if (period > 0)
            {
                discountFactor *= (1 + discountRate);
            }

            npv += cashFlow / discountFactor;
            period++;
        }

        return npv;
    }

    // ── Internal Rate of Return (IRR) ────────────────────────────────────

    /// <summary>
    /// Approximates the Internal Rate of Return using the Newton-Raphson method.
    /// </summary>
    /// <param name="cashFlows">Cash flows where index 0 is the initial investment.</param>
    /// <param name="maxIterations">Maximum Newton-Raphson iterations.</param>
    /// <param name="tolerance">Convergence tolerance.</param>
    /// <returns>IRR as a decimal (e.g. 0.12 for 12%).</returns>
    public static decimal InternalRateOfReturn(
        IEnumerable<decimal> cashFlows,
        int maxIterations = 1000,
        decimal tolerance = 0.0000001m)
    {
        var flows = cashFlows as IList<decimal> ?? cashFlows.ToList();
        if (flows.Count < 2) throw new ArgumentException("At least two cash flows are required.", nameof(cashFlows));

        decimal rate = 0.1m; // Initial guess: 10%

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            decimal npv = 0m;
            decimal dNpv = 0m; // Derivative of NPV with respect to rate

            for (int t = 0; t < flows.Count; t++)
            {
                decimal discountFactor = 1m;
                for (int p = 0; p < t; p++)
                {
                    discountFactor *= (1 + rate);
                }

                npv += flows[t] / discountFactor;
                dNpv -= t * flows[t] / (discountFactor * (1 + rate));
            }

            if (Math.Abs(dNpv) < 1e-20m)
            {
                break; // Avoid division by near-zero
            }

            decimal newRate = rate - npv / dNpv;

            if (Math.Abs(newRate - rate) < tolerance)
            {
                return newRate;
            }

            rate = newRate;
        }

        return rate; // Return best approximation
    }

    // ── Currency & Rounding ──────────────────────────────────────────────

    // ── Extended Internal Rate of Return (XIRR) ─────────────────────────

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for irregular cash flows
    /// using the Newton-Raphson method. Cash flows are date-based rather than period-based.
    /// </summary>
    /// <param name="cashFlows">List of (Date, Amount) tuples. Negative amounts represent outflows.</param>
    /// <param name="guess">Initial rate guess (default 0.1 = 10%).</param>
    /// <returns>XIRR as a double (e.g. 0.12 for 12%).</returns>
    public static double CalculateXIRR(List<(DateTime Date, double Amount)> cashFlows, double guess = 0.1)
    {
        if (cashFlows == null || cashFlows.Count < 2)
            return 0;

        var firstDate = cashFlows.Min(cf => cf.Date);
        var rate = guess;

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            double fx = 0;
            double dfx = 0;

            for (int i = 0; i < cashFlows.Count; i++)
            {
                var days = (cashFlows[i].Date - firstDate).TotalDays / 365.0;
                var factor = Math.Pow(1.0 + rate, days);
                fx += cashFlows[i].Amount / factor;
                dfx -= cashFlows[i].Amount * days / (factor * (1.0 + rate));
            }

            if (Math.Abs(fx) < 1e-10)
                return rate;

            if (Math.Abs(dfx) < 1e-10)
                break;

            rate -= fx / dfx;
        }

        return rate;
    }

    // ── Compound Interest with Regular Contributions (SIP-style) ───────

    /// <summary>
    /// Calculates the future value with compound interest and regular monthly contributions (SIP-style).
    /// </summary>
    /// <param name="principal">Initial principal amount.</param>
    /// <param name="annualRate">Annual interest rate as a percentage (e.g. 8.5 for 8.5%).</param>
    /// <param name="totalMonths">Total number of months.</param>
    /// <param name="monthlyContribution">Monthly contribution amount.</param>
    /// <returns>Future value after compounding.</returns>
    public static decimal CompoundInterest(decimal principal, decimal annualRate, int totalMonths, decimal monthlyContribution)
    {
        var monthlyRate = (double)(annualRate / 100 / 12);
        var months = totalMonths;
        double balance = (double)principal;

        for (int m = 0; m < months; m++)
        {
            balance = balance * (1 + monthlyRate) + (double)monthlyContribution;
        }

        return (decimal)balance;
    }

    // ── Currency & Rounding ──────────────────────────────────────────────

    /// <summary>
    /// Rounds a monetary amount to 2 decimal places using midpoint rounding (banker's rounding).
    /// </summary>
    public static decimal RoundMoney(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Rounds a monetary amount to the specified number of decimal places.
    /// </summary>
    public static decimal RoundMoney(decimal amount, int decimals)
    {
        return Math.Round(amount, decimals, MidpointRounding.ToEven);
    }

    // ── Percentage Calculations ──────────────────────────────────────────

    /// <summary>
    /// Calculates the percentage change from an old value to a new value.
    /// Returns a decimal (e.g. 0.05 for 5% increase, -0.05 for 5% decrease).
    /// </summary>
    public static decimal PercentageChange(decimal oldValue, decimal newValue)
    {
        if (oldValue == 0) throw new ArgumentException("Old value cannot be zero.", nameof(oldValue));
        return (newValue - oldValue) / Math.Abs(oldValue);
    }

    /// <summary>
    /// Calculates what percentage <paramref name="part"/> is of <paramref name="whole"/>.
    /// </summary>
    public static decimal PercentageOf(decimal part, decimal whole)
    {
        if (whole == 0) throw new ArgumentException("Whole cannot be zero.", nameof(whole));
        return part / whole;
    }
}
