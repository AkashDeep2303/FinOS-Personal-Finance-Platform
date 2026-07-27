using FinOS.CoreFinance.Application.Commands;
using FinOS.CoreFinance.Application.Validators;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class TaxRuleValidationTests
{
    [Fact]
    public async Task Validator_AcceptsContiguousVersionedSlabs()
    {
        var command = Command("""
            {"slabIncomeTypes":["salary","interest"],"slabs":[
              {"lowerLimit":0,"upperLimit":300000,"ratePct":0},
              {"lowerLimit":300000,"upperLimit":null,"ratePct":10}
            ]}
            """);

        var result = await new CreateTaxRuleVersionValidator().ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("""{"slabIncomeTypes":["salary"],"slabs":[{"lowerLimit":100,"upperLimit":null,"ratePct":10}]}""")]
    [InlineData("""{"slabIncomeTypes":["salary"],"slabs":[{"lowerLimit":0,"upperLimit":300000,"ratePct":0},{"lowerLimit":250000,"upperLimit":null,"ratePct":10}]}""")]
    [InlineData("""{"slabIncomeTypes":["salary"],"slabs":[{"lowerLimit":0,"upperLimit":null,"ratePct":101}]}""")]
    [InlineData("""{"slabs":[{"lowerLimit":0,"upperLimit":null,"ratePct":10}]}""")]
    public async Task Validator_RejectsUnsafeSlabConfiguration(string configuration)
    {
        var result = await new CreateTaxRuleVersionValidator().ValidateAsync(Command(configuration));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_RejectsAssessmentYearThatDoesNotFollowFinancialYear()
    {
        var command = Command("""{"slabIncomeTypes":["salary"],"slabs":[{"lowerLimit":0,"upperLimit":null,"ratePct":0}]}""")
            with { AssessmentYear = "2029-30" };

        var result = await new CreateTaxRuleVersionValidator().ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    private static CreateTaxRuleVersionCommand Command(string configuration) =>
        new("2026-27", "2027-28", "New", "v1", configuration,
            new DateTime(2026, 4, 1), new DateTime(2027, 3, 31));
}
