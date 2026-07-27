namespace FinOS.CoreFinance.Application.DTOs;

public sealed record TaxProjectionBreakdownDto(
    string Regime, bool Available, long? RuleVersionId, string? RuleVersion,
    decimal GrossIncome, decimal TaxableIncome, decimal BaseTax, decimal Rebate,
    decimal Cess, decimal EstimatedTax, decimal TaxesPaid,
    decimal EstimatedPayableOrRefund, IReadOnlyList<string> Warnings);

public sealed record TaxRegimeComparisonDto(
    string FinancialYear, TaxProjectionBreakdownDto Old, TaxProjectionBreakdownDto New,
    string? LowerEstimatedTaxRegime, string Explanation);

public sealed record TaxCalculationResult(
    decimal GrossIncome, decimal TaxableIncome, decimal BaseTax, decimal Rebate,
    decimal Cess, decimal EstimatedTax, decimal TaxesPaid,
    decimal EstimatedPayableOrRefund, IReadOnlyList<string> Warnings);
