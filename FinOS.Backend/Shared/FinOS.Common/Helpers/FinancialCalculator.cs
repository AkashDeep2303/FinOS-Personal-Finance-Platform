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

        // Use the algebraically equivalent inverse-power form so long terms
        // do not overflow Decimal while calculating (1 + rate)^term.
        var monthlyRate = (double)(annualRate / 12m);
        var discountFactor = Math.Pow(1d + monthlyRate, -termInMonths);
        var denominator = 1d - discountFactor;
        var emi = (double)principal * monthlyRate / denominator;

        if (!double.IsFinite(emi) || emi <= 0d || emi > (double)decimal.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(annualRate), "The rate and term produce an unsupported EMI.");

        return (decimal)emi;
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
        if (futureValue < 0) throw new ArgumentException("Future value cannot be negative.", nameof(futureValue));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (timeInYears < 0) throw new ArgumentException("Time cannot be negative.", nameof(timeInYears));

        var discountFactor = (decimal)Math.Pow((double)(1m + annualRate), (double)timeInYears);
        return RoundMoney(futureValue / discountFactor);
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

    /// <summary>
    /// Calculates XIRR while preserving decimal cash-flow amounts and reporting convergence failure.
    /// </summary>
    public static decimal ExtendedInternalRateOfReturn(
        IReadOnlyCollection<(DateTime Date, decimal Amount)> cashFlows,
        decimal guess = 0.10m,
        int maxIterations = 100,
        decimal tolerance = 0.00000001m)
    {
        if (cashFlows is null || cashFlows.Count < 2)
            throw new ArgumentException("At least two cash flows are required.", nameof(cashFlows));
        if (!cashFlows.Any(x => x.Amount < 0) || !cashFlows.Any(x => x.Amount > 0))
            throw new ArgumentException("Cash flows must contain at least one inflow and one outflow.", nameof(cashFlows));
        if (guess <= -1m) throw new ArgumentException("Guess must be greater than -100%.", nameof(guess));

        var firstDate = cashFlows.Min(x => x.Date);
        var rate = guess;
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            decimal value = 0;
            decimal derivative = 0;
            foreach (var cashFlow in cashFlows)
            {
                var years = (decimal)(cashFlow.Date - firstDate).TotalDays / 365m;
                var factor = (decimal)Math.Pow((double)(1m + rate), (double)years);
                value += cashFlow.Amount / factor;
                derivative -= cashFlow.Amount * years /
                    (decimal)Math.Pow((double)(1m + rate), (double)(years + 1m));
            }

            if (Math.Abs(value) <= tolerance) return Math.Round(rate, 8, MidpointRounding.ToEven);
            if (Math.Abs(derivative) <= tolerance)
                throw new InvalidOperationException("XIRR could not converge because the derivative approached zero.");

            var next = rate - value / derivative;
            if (next <= -1m) next = (rate - 1m) / 2m;
            if (Math.Abs(next - rate) <= tolerance) return Math.Round(next, 8, MidpointRounding.ToEven);
            rate = next;
        }

        throw new InvalidOperationException("XIRR did not converge within the configured iteration limit.");
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

    /// <summary>Calculates net worth as assets less liabilities.</summary>
    public static decimal NetWorth(decimal totalAssets, decimal totalLiabilities)
    {
        if (totalAssets < 0) throw new ArgumentException("Assets cannot be negative.", nameof(totalAssets));
        if (totalLiabilities < 0) throw new ArgumentException("Liabilities cannot be negative.", nameof(totalLiabilities));
        return RoundMoney(totalAssets - totalLiabilities);
    }

    /// <summary>Calculates savings as a proportion of income.</summary>
    public static decimal SavingsRate(decimal income, decimal expenses)
    {
        if (income < 0) throw new ArgumentException("Income cannot be negative.", nameof(income));
        if (expenses < 0) throw new ArgumentException("Expenses cannot be negative.", nameof(expenses));
        return income == 0 ? 0 : Math.Round((income - expenses) / income, 6, MidpointRounding.ToEven);
    }

    /// <summary>Calculates monthly debt obligations as a proportion of gross monthly income.</summary>
    public static decimal DebtToIncomeRatio(decimal monthlyDebtPayments, decimal grossMonthlyIncome)
    {
        if (monthlyDebtPayments < 0) throw new ArgumentException("Debt payments cannot be negative.", nameof(monthlyDebtPayments));
        if (grossMonthlyIncome < 0) throw new ArgumentException("Income cannot be negative.", nameof(grossMonthlyIncome));
        return grossMonthlyIncome == 0 ? 0 : Math.Round(monthlyDebtPayments / grossMonthlyIncome, 6, MidpointRounding.ToEven);
    }

    /// <summary>Calculates the number of months covered by liquid emergency funds.</summary>
    public static decimal EmergencyFundCoverage(decimal liquidEmergencyFunds, decimal essentialMonthlyExpenses)
    {
        if (liquidEmergencyFunds < 0) throw new ArgumentException("Emergency funds cannot be negative.", nameof(liquidEmergencyFunds));
        if (essentialMonthlyExpenses < 0) throw new ArgumentException("Essential expenses cannot be negative.", nameof(essentialMonthlyExpenses));
        return essentialMonthlyExpenses == 0 ? 0 : Math.Round(liquidEmergencyFunds / essentialMonthlyExpenses, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Calculates credit-card payoff duration and interest using a fixed
    /// month-end payment. The annual rate is an absolute decimal.
    /// </summary>
    public static (int Months, decimal TotalInterest) CreditCardPayoff(
        decimal balance, decimal annualRate, decimal monthlyPayment, int maximumMonths = 1200)
    {
        if (balance <= 0) throw new ArgumentException("Balance must be positive.", nameof(balance));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (monthlyPayment <= 0) throw new ArgumentException("Payment must be positive.", nameof(monthlyPayment));
        if (maximumMonths <= 0) throw new ArgumentException("Maximum months must be positive.", nameof(maximumMonths));

        var monthlyRate = annualRate / 12m;
        if (monthlyRate > 0 && monthlyPayment <= balance * monthlyRate)
            throw new ArgumentException("Payment must exceed the first month's interest.", nameof(monthlyPayment));

        var remaining = balance;
        var interestPaid = 0m;
        var months = 0;
        while (remaining > 0.005m && months < maximumMonths)
        {
            var interest = RoundMoney(remaining * monthlyRate);
            interestPaid += interest;
            remaining = Math.Max(0, remaining + interest - monthlyPayment);
            months++;
        }

        if (remaining > 0.005m)
            throw new InvalidOperationException("Balance does not amortize within the configured maximum duration.");
        return (months, RoundMoney(interestPaid));
    }

    /// <summary>Calculates compound annual growth rate using decimal rate output.</summary>
    public static decimal CompoundAnnualGrowthRate(decimal beginningValue, decimal endingValue, decimal years)
    {
        if (beginningValue <= 0) throw new ArgumentException("Beginning value must be positive.", nameof(beginningValue));
        if (endingValue < 0) throw new ArgumentException("Ending value cannot be negative.", nameof(endingValue));
        if (years <= 0) throw new ArgumentException("Years must be positive.", nameof(years));
        if (endingValue == 0) return -1m;

        var rate = Math.Pow((double)(endingValue / beginningValue), 1d / (double)years) - 1d;
        return Math.Round((decimal)rate, 8, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Calculates the monthly contribution required to reach a future target.
    /// The annual return is an absolute decimal and contributions occur at month end.
    /// </summary>
    public static decimal RequiredMonthlyContribution(
        decimal targetAmount,
        decimal currentAmount,
        decimal annualRate,
        int months)
    {
        if (targetAmount < 0) throw new ArgumentException("Target cannot be negative.", nameof(targetAmount));
        if (currentAmount < 0) throw new ArgumentException("Current amount cannot be negative.", nameof(currentAmount));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (months <= 0) throw new ArgumentException("Months must be positive.", nameof(months));

        var monthlyRate = annualRate / 12m;
        var growthFactor = 1m;
        for (var month = 0; month < months; month++) growthFactor *= 1m + monthlyRate;

        var remainingFutureValue = targetAmount - (currentAmount * growthFactor);
        if (remainingFutureValue <= 0) return 0;
        if (monthlyRate == 0) return RoundMoney(remainingFutureValue / months);

        var annuityFactor = (growthFactor - 1m) / monthlyRate;
        return RoundMoney(remainingFutureValue / annuityFactor);
    }

    /// <summary>
    /// Calculates the future value of an opening corpus plus month-end contributions.
    /// The annual return is an absolute decimal.
    /// </summary>
    public static decimal FutureValueWithMonthlyContributions(
        decimal openingCorpus,
        decimal monthlyContribution,
        decimal annualRate,
        int months)
    {
        if (openingCorpus < 0) throw new ArgumentException("Opening corpus cannot be negative.", nameof(openingCorpus));
        if (monthlyContribution < 0) throw new ArgumentException("Contribution cannot be negative.", nameof(monthlyContribution));
        if (annualRate < 0) throw new ArgumentException("Rate cannot be negative.", nameof(annualRate));
        if (months < 0) throw new ArgumentException("Months cannot be negative.", nameof(months));

        var monthlyRate = annualRate / 12m;
        var balance = openingCorpus;
        for (var month = 0; month < months; month++)
            balance = balance * (1m + monthlyRate) + monthlyContribution;

        return RoundMoney(balance);
    }

    /// <summary>Inflates a present amount over a number of years.</summary>
    public static decimal InflationAdjustedValue(decimal presentValue, decimal annualInflation, int years)
    {
        if (presentValue < 0) throw new ArgumentException("Present value cannot be negative.", nameof(presentValue));
        if (annualInflation < 0) throw new ArgumentException("Inflation cannot be negative.", nameof(annualInflation));
        if (years < 0) throw new ArgumentException("Years cannot be negative.", nameof(years));

        var value = presentValue;
        for (var year = 0; year < years; year++) value *= 1m + annualInflation;
        return RoundMoney(value);
    }

    /// <summary>
    /// Calculates the corpus required at retirement to fund month-end expenses for a fixed duration.
    /// Returns are nominal absolute decimals and expenses rise with inflation.
    /// </summary>
    public static decimal RetirementCorpus(
        decimal firstMonthlyExpense,
        decimal annualPostRetirementReturn,
        decimal annualInflation,
        int retirementMonths)
    {
        if (firstMonthlyExpense < 0) throw new ArgumentException("Expense cannot be negative.", nameof(firstMonthlyExpense));
        if (annualPostRetirementReturn < 0) throw new ArgumentException("Return cannot be negative.", nameof(annualPostRetirementReturn));
        if (annualInflation < 0) throw new ArgumentException("Inflation cannot be negative.", nameof(annualInflation));
        if (retirementMonths <= 0) throw new ArgumentException("Retirement duration must be positive.", nameof(retirementMonths));

        var monthlyReturn = annualPostRetirementReturn / 12m;
        var monthlyInflation = annualInflation / 12m;
        var corpus = 0m;
        var expense = firstMonthlyExpense;
        var discountFactor = 1m;
        for (var month = 1; month <= retirementMonths; month++)
        {
            discountFactor *= 1m + monthlyReturn;
            corpus += expense / discountFactor;
            expense *= 1m + monthlyInflation;
        }

        return RoundMoney(corpus);
    }
}
