using FinOS.CoreFinance.Application.Services;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class TaxProjectionCalculatorTests
{
    [Fact]
    public void Calculate_UsesOnlyConfiguredIncomeTreatments()
    {
        const string input = """
            {"salary":500000,"capitalGains":100000,"otherIncome":25000,
             "deductions":100000,"tdsPaid":10000,"otherTaxPaid":0}
            """;
        const string rule = """
            {"slabIncomeTypes":["salary"],"specialIncomeRates":{"capitalGains":10},
             "deductionLimit":50000,"cessRatePct":4,
             "slabs":[{"lowerLimit":0,"upperLimit":300000,"ratePct":0},
                      {"lowerLimit":300000,"upperLimit":null,"ratePct":10}]}
            """;

        var result = TaxProjectionCalculator.Calculate(input, rule);

        Assert.Equal(625000m, result.GrossIncome);
        Assert.Equal(450000m, result.TaxableIncome);
        Assert.Equal(25000m, result.BaseTax);
        Assert.Equal(1000m, result.Cess);
        Assert.Equal(26000m, result.EstimatedTax);
        Assert.Equal(16000m, result.EstimatedPayableOrRefund);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Calculate_AppliesConfiguredRebateWithoutProducingNegativeTax()
    {
        const string input = """{"salary":200000}""";
        const string rule = """
            {"slabIncomeTypes":["salary"],"rebateThreshold":300000,"rebateAmount":999999,
             "slabs":[{"lowerLimit":0,"upperLimit":null,"ratePct":10}]}
            """;

        var result = TaxProjectionCalculator.Calculate(input, rule);

        Assert.Equal(0m, result.EstimatedTax);
        Assert.Equal(20000m, result.Rebate);
    }
}
