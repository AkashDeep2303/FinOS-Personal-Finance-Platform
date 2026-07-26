using Dapper;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
namespace FinOS.CoreFinance.Infrastructure.Repositories;
public class TaxRepository(IConnectionFactory factory) : ITaxRepository
{
    public async Task<TaxProfile?> GetProfileAsync(long userId,string fy,CancellationToken ct=default)
    { using var c=factory.CreateConnection(); return await c.QueryFirstOrDefaultAsync<TaxProfile>("SELECT Id,UserId,FinancialYear,PreferredRegime,InputJson,UpdatedAt FROM Tax.Profiles WHERE UserId=@userId AND FinancialYear=@fy AND DeletedAt IS NULL",new{userId,fy}); }
    public async Task<TaxProfile> UpsertProfileAsync(TaxProfile p,CancellationToken ct=default)
    { using var c=factory.CreateConnection(); p.Id=await c.ExecuteScalarAsync<long>(@"UPDATE Tax.Profiles SET PreferredRegime=@PreferredRegime,InputJson=@InputJson,UpdatedAt=SYSUTCDATETIME(),DeletedAt=NULL WHERE UserId=@UserId AND FinancialYear=@FinancialYear;
IF @@ROWCOUNT=0 INSERT Tax.Profiles(UserId,FinancialYear,PreferredRegime,InputJson) VALUES(@UserId,@FinancialYear,@PreferredRegime,@InputJson);
SELECT Id FROM Tax.Profiles WHERE UserId=@UserId AND FinancialYear=@FinancialYear;",p); return p; }
    public async Task<IReadOnlyList<(long,string,string,string,string)>> GetPublishedRulesAsync(string fy,CancellationToken ct=default)
    { using var c=factory.CreateConnection(); var rows=await c.QueryAsync<(long,string,string,string,string)>("SELECT Id,FinancialYear,AssessmentYear,Regime,Version FROM Tax.RuleVersions WHERE FinancialYear=@fy AND IsPublished=1 AND EffectiveFrom<=CAST(SYSUTCDATETIME() AS date) AND (EffectiveTo IS NULL OR EffectiveTo>=CAST(SYSUTCDATETIME() AS date))",new{fy}); return rows.ToList(); }

    public async Task<TaxRuleVersion> CreateRuleVersionAsync(TaxRuleVersion rule, CancellationToken ct = default)
    {
        const string sql = """
            INSERT Tax.RuleVersions
                (FinancialYear, AssessmentYear, Regime, Version, ConfigurationJson,
                 EffectiveFrom, EffectiveTo, IsPublished)
            OUTPUT INSERTED.Id, INSERTED.FinancialYear, INSERTED.AssessmentYear,
                   INSERTED.Regime, INSERTED.Version, INSERTED.ConfigurationJson,
                   INSERTED.EffectiveFrom, INSERTED.EffectiveTo,
                   INSERTED.IsPublished, INSERTED.CreatedAt
            VALUES
                (@FinancialYear, @AssessmentYear, @Regime, @Version,
                 @ConfigurationJson, @EffectiveFrom, @EffectiveTo, 0);
            """;
        using var connection = factory.CreateConnection();
        return await connection.QuerySingleAsync<TaxRuleVersion>(
            new CommandDefinition(sql, rule, cancellationToken: ct));
    }

    public async Task<TaxRuleVersion?> PublishRuleVersionAsync(long id, CancellationToken ct = default)
    {
        using var connection = factory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var rule = await connection.QuerySingleOrDefaultAsync<TaxRuleVersion>(
            new CommandDefinition("""
                SELECT Id, FinancialYear, AssessmentYear, Regime, Version,
                       ConfigurationJson, EffectiveFrom, EffectiveTo,
                       IsPublished, CreatedAt
                FROM Tax.RuleVersions WITH (UPDLOCK, HOLDLOCK)
                WHERE Id = @Id;
                """, new { Id = id }, transaction, cancellationToken: ct));
        if (rule is null) return null;

        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE Tax.RuleVersions
            SET IsPublished = 0
            WHERE FinancialYear = @FinancialYear AND Regime = @Regime
              AND Id <> @Id AND IsPublished = 1;
            UPDATE Tax.RuleVersions SET IsPublished = 1 WHERE Id = @Id;
            """, new { rule.FinancialYear, rule.Regime, Id = id }, transaction, cancellationToken: ct));
        transaction.Commit();
        rule.IsPublished = true;
        return rule;
    }

    public async Task<TaxRuleVersion?> GetPublishedRuleAsync(string financialYear, string regime, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (1) Id, FinancialYear, AssessmentYear, Regime, Version,
                   ConfigurationJson, EffectiveFrom, EffectiveTo, IsPublished, CreatedAt
            FROM Tax.RuleVersions
            WHERE FinancialYear = @financialYear AND Regime = @regime
              AND IsPublished = 1
              AND EffectiveFrom <= CAST(SYSUTCDATETIME() AS date)
              AND (EffectiveTo IS NULL OR EffectiveTo >= CAST(SYSUTCDATETIME() AS date))
            ORDER BY CreatedAt DESC;
            """;
        using var connection = factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<TaxRuleVersion>(
            new CommandDefinition(sql, new { financialYear, regime }, cancellationToken: ct));
    }

    public async Task<TaxProjection> AddProjectionAsync(TaxProjection projection, CancellationToken ct = default)
    {
        const string sql = """
            INSERT Tax.Projections
                (UserId, TaxProfileId, TaxRuleVersionId, GrossIncome, TaxableIncome,
                 EstimatedTax, TaxesPaid, EstimatedPayableOrRefund, CalculationJson)
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.TaxProfileId,
                   INSERTED.TaxRuleVersionId, INSERTED.GrossIncome,
                   INSERTED.TaxableIncome, INSERTED.EstimatedTax, INSERTED.TaxesPaid,
                   INSERTED.EstimatedPayableOrRefund, INSERTED.CalculationJson,
                   INSERTED.CalculatedAt
            VALUES
                (@UserId, @TaxProfileId, @TaxRuleVersionId, @GrossIncome,
                 @TaxableIncome, @EstimatedTax, @TaxesPaid,
                 @EstimatedPayableOrRefund, @CalculationJson);
            """;
        using var connection = factory.CreateConnection();
        return await connection.QuerySingleAsync<TaxProjection>(
            new CommandDefinition(sql, projection, cancellationToken: ct));
    }
}
