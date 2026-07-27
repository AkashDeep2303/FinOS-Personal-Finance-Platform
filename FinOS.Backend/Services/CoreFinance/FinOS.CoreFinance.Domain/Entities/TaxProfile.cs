namespace FinOS.CoreFinance.Domain.Entities;
public class TaxProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FinancialYear { get; set; } = "";
    public string? PreferredRegime { get; set; }
    public string InputJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}

public sealed class TaxRuleVersion
{
    public long Id { get; set; }
    public string FinancialYear { get; set; } = "";
    public string AssessmentYear { get; set; } = "";
    public string Regime { get; set; } = "";
    public string Version { get; set; } = "";
    public string ConfigurationJson { get; set; } = "{}";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TaxProjection
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long TaxProfileId { get; set; }
    public long TaxRuleVersionId { get; set; }
    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal EstimatedTax { get; set; }
    public decimal TaxesPaid { get; set; }
    public decimal EstimatedPayableOrRefund { get; set; }
    public string CalculationJson { get; set; } = "{}";
    public DateTime CalculatedAt { get; set; }
}
