using FinOS.CoreFinance.Application.DTOs;
using System.Text.Json;

namespace FinOS.CoreFinance.Application.Services;

public static class TaxProjectionCalculator
{
    private static readonly string[] IncomeTypes =
        ["salary", "interest", "dividend", "rentalIncome", "capitalGains", "otherIncome"];

    public static TaxCalculationResult Calculate(string inputJson, string configurationJson)
    {
        using var input = JsonDocument.Parse(inputJson);
        using var config = JsonDocument.Parse(configurationJson);
        var root = config.RootElement;
        var taxableTypes = root.GetProperty("slabIncomeTypes").EnumerateArray()
            .Select(x => x.GetString() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var specialRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("specialIncomeRates", out var special) &&
            special.ValueKind == JsonValueKind.Object)
            foreach (var item in special.EnumerateObject()) specialRates[item.Name] = item.Value.GetDecimal();

        decimal gross = 0, slabIncome = 0, specialTax = 0;
        var warnings = new List<string>();
        foreach (var incomeType in IncomeTypes)
        {
            var amount = ReadNonNegative(input.RootElement, incomeType);
            gross += amount;
            if (taxableTypes.Contains(incomeType)) slabIncome += amount;
            else if (specialRates.TryGetValue(incomeType, out var rate)) specialTax += amount * rate / 100m;
            else if (amount > 0) warnings.Add($"{incomeType} was excluded because this rule does not configure its tax treatment.");
        }

        var deduction = ReadNonNegative(input.RootElement, "deductions");
        var deductionLimit = ReadOptionalNonNegative(root, "deductionLimit");
        if (deductionLimit.HasValue) deduction = Math.Min(deduction, deductionLimit.Value);
        else if (deduction > 0) warnings.Add("Deductions were excluded because this rule does not configure a deduction limit.");
        if (!deductionLimit.HasValue) deduction = 0;

        var taxableIncome = Math.Max(0, slabIncome - deduction);
        decimal slabTax = 0;
        foreach (var slab in root.GetProperty("slabs").EnumerateArray())
        {
            var lower = slab.GetProperty("lowerLimit").GetDecimal();
            decimal? upper = slab.TryGetProperty("upperLimit", out var upperValue) &&
                             upperValue.ValueKind != JsonValueKind.Null
                ? upperValue.GetDecimal() : null;
            var amount = Math.Max(0, Math.Min(taxableIncome, upper ?? taxableIncome) - lower);
            slabTax += amount * slab.GetProperty("ratePct").GetDecimal() / 100m;
        }

        var baseTax = Round(slabTax + specialTax);
        decimal rebate = 0;
        if (root.TryGetProperty("rebateThreshold", out var threshold) &&
            root.TryGetProperty("rebateAmount", out var rebateAmount) &&
            taxableIncome <= threshold.GetDecimal())
            rebate = Math.Min(baseTax, rebateAmount.GetDecimal());
        var cessRate = ReadOptionalNonNegative(root, "cessRatePct") ?? 0;
        var cess = Round((baseTax - rebate) * cessRate / 100m);
        var estimatedTax = Round(baseTax - rebate + cess);
        var taxesPaid = Round(ReadNonNegative(input.RootElement, "tdsPaid") +
                              ReadNonNegative(input.RootElement, "otherTaxPaid"));
        return new(gross, taxableIncome, baseTax, Round(rebate), cess, estimatedTax,
            taxesPaid, Round(estimatedTax - taxesPaid), warnings);
    }

    private static decimal ReadNonNegative(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var number) && number > 0 ? number : 0;

    private static decimal? ReadOptionalNonNegative(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var number) && number >= 0 ? number : null;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
