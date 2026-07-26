using FinOS.CoreFinance.Domain.Entities;
namespace FinOS.CoreFinance.Domain.Interfaces;
public interface ITaxRepository
{
    Task<TaxProfile?> GetProfileAsync(long userId, string financialYear, CancellationToken ct = default);
    Task<TaxProfile> UpsertProfileAsync(TaxProfile profile, CancellationToken ct = default);
    Task<IReadOnlyList<(long Id,string FinancialYear,string AssessmentYear,string Regime,string Version)>> GetPublishedRulesAsync(string financialYear, CancellationToken ct = default);
    Task<TaxRuleVersion> CreateRuleVersionAsync(TaxRuleVersion rule, CancellationToken ct = default);
    Task<TaxRuleVersion?> PublishRuleVersionAsync(long id, CancellationToken ct = default);
    Task<TaxRuleVersion?> GetPublishedRuleAsync(string financialYear, string regime, CancellationToken ct = default);
    Task<TaxProjection> AddProjectionAsync(TaxProjection projection, CancellationToken ct = default);
}
