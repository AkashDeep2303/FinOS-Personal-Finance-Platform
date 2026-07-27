using FinOS.Common.Helpers;
using Xunit;

namespace FinOS.Common.Tests;

public class FinancialCalculatorTests
{
    [Theory]
    [InlineData(100000, 0, 10, 10000)]
    [InlineData(1000000, 0.09, 240, 8997.26)]
    public void CalculateEmi_ReturnsExpectedAmount(decimal principal, decimal rate, int months, decimal expected)
    {
        var result = FinancialCalculator.RoundMoney(FinancialCalculator.CalculateEMI(principal, rate, months));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateEmi_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentException>(() => FinancialCalculator.CalculateEMI(0, 0.08m, 12));
        Assert.Throws<ArgumentException>(() => FinancialCalculator.CalculateEMI(1000, -0.01m, 12));
        Assert.Throws<ArgumentException>(() => FinancialCalculator.CalculateEMI(1000, 0.01m, 0));
    }

    [Fact]
    public void CalculateEmi_SupportsHighRateAndLongTermWithoutIntermediateOverflow()
    {
        var result = FinancialCalculator.CalculateEMI(1_000_000m, 1m, 1200);

        Assert.InRange(result, 83_333m, 83_334m);
    }

    [Theory]
    [InlineData(100000, 60000, 0.4)]
    [InlineData(0, 0, 0)]
    [InlineData(100000, 120000, -0.2)]
    public void SavingsRate_IsDeterministic(decimal income, decimal expenses, decimal expected) =>
        Assert.Equal(expected, FinancialCalculator.SavingsRate(income, expenses));

    [Fact]
    public void Ratios_HandleZeroDenominators()
    {
        Assert.Equal(0, FinancialCalculator.DebtToIncomeRatio(0, 0));
        Assert.Equal(0, FinancialCalculator.EmergencyFundCoverage(100000, 0));
    }

    [Fact]
    public void CoreFinancialMetrics_ReturnExpectedValues()
    {
        Assert.Equal(600000, FinancialCalculator.NetWorth(1000000, 400000));
        Assert.Equal(0.25m, FinancialCalculator.DebtToIncomeRatio(25000, 100000));
        Assert.Equal(6m, FinancialCalculator.EmergencyFundCoverage(300000, 50000));
        Assert.Equal(0.10m, FinancialCalculator.CompoundAnnualGrowthRate(100000, 121000, 2));
    }

    [Fact]
    public void RequiredMonthlyContribution_HandlesZeroRateAndFundedGoal()
    {
        Assert.Equal(7500m, FinancialCalculator.RequiredMonthlyContribution(100000, 10000, 0, 12));
        Assert.Equal(0m, FinancialCalculator.RequiredMonthlyContribution(100000, 100000, 0.08m, 12));
    }

    [Fact]
    public void RequiredMonthlyContribution_SupportsLongDuration()
    {
        var result = FinancialCalculator.RequiredMonthlyContribution(10000000, 500000, 0.10m, 360);
        Assert.InRange(result, 0.01m, 100m);
    }

    [Fact]
    public void FutureValueWithMonthlyContributions_HandlesZeroReturn()
    {
        Assert.Equal(220000m, FinancialCalculator.FutureValueWithMonthlyContributions(100000, 10000, 0, 12));
    }

    [Fact]
    public void InflationAdjustedValue_CompoundsAnnually()
    {
        Assert.Equal(112360m, FinancialCalculator.InflationAdjustedValue(100000, 0.06m, 2));
    }

    [Fact]
    public void RetirementCorpus_ReturnsFinitePositiveCorpus()
    {
        var corpus = FinancialCalculator.RetirementCorpus(100000, 0.07m, 0.05m, 300);
        Assert.InRange(corpus, 15000000m, 30000000m);
    }

    [Fact]
    public void RetirementCalculations_RejectInvalidDurations()
    {
        Assert.Throws<ArgumentException>(() => FinancialCalculator.RetirementCorpus(100000, 0.07m, 0.05m, 0));
        Assert.Throws<ArgumentException>(() => FinancialCalculator.FutureValueWithMonthlyContributions(0, 1000, 0.08m, -1));
    }

    [Fact]
    public void PresentValue_SupportsFractionalYears()
    {
        var result = FinancialCalculator.PresentValue(121000m, 0.10m, 1.5m);
        Assert.Equal(104880.88m, result);
    }

    [Fact]
    public void DecimalXirr_ReturnsAnnualizedRate()
    {
        var flows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2025, 1, 1), -100000m),
            (new DateTime(2026, 1, 1), 110000m)
        };
        Assert.Equal(0.10m, FinancialCalculator.ExtendedInternalRateOfReturn(flows));
    }

    [Fact]
    public void DecimalXirr_RejectsOneSidedCashFlows()
    {
        var flows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2025, 1, 1), -100000m),
            (new DateTime(2026, 1, 1), -10000m)
        };
        Assert.Throws<ArgumentException>(() => FinancialCalculator.ExtendedInternalRateOfReturn(flows));
    }

    [Fact]
    public void FixedDepositStyleQuarterlyCompounding_ReturnsExpectedMaturity()
    {
        var maturity = FinancialCalculator.RoundMoney(
            FinancialCalculator.CompoundInterestFutureValue(100000m, 0.08m, 1m, 4));

        Assert.Equal(108243.22m, maturity);
    }

    [Fact]
    public void CompoundAnnualGrowthRate_RejectsInvalidBeginningValueAndDuration()
    {
        Assert.Throws<ArgumentException>(() => FinancialCalculator.CompoundAnnualGrowthRate(0, 100000, 1));
        Assert.Throws<ArgumentException>(() => FinancialCalculator.CompoundAnnualGrowthRate(100000, 110000, 0));
    }

    [Fact]
    public void CreditCardPayoff_CalculatesDurationAndInterest()
    {
        var result = FinancialCalculator.CreditCardPayoff(100000m, 0.36m, 10000m);

        Assert.Equal(13, result.Months);
        Assert.Equal(20675.46m, result.TotalInterest);
    }

    [Fact]
    public void CreditCardPayoff_RejectsPaymentThatDoesNotAmortize()
    {
        Assert.Throws<ArgumentException>(() =>
            FinancialCalculator.CreditCardPayoff(100000m, 0.36m, 3000m));
    }
}
