using Dapper;
using FinOS.Analytics.Domain.Entities;
using FinOS.Analytics.Domain.Interfaces;
using FinOS.Common.Interfaces;

namespace FinOS.Analytics.Infrastructure.Repositories;

public class ScenarioRepository(IConnectionFactory connectionFactory) : IScenarioRepository
{
    public async Task<Scenario> AddAsync(Scenario scenario, CancellationToken ct = default)
    {
        using var connection = connectionFactory.CreateConnection();
        scenario.Id = await connection.ExecuteScalarAsync<long>(
            @"INSERT INTO Analytics.Scenarios (UserId, Name, ScenarioType, Verdict, InputJson, ResultJson)
              VALUES (@UserId, @Name, @ScenarioType, @Verdict, @InputJson, @ResultJson);
              SELECT CAST(SCOPE_IDENTITY() AS BIGINT);", scenario);
        return scenario;
    }

    public async Task<IReadOnlyList<Scenario>> GetByUserAsync(long userId, CancellationToken ct = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Scenario>(
            @"SELECT Id, UserId, Name, ScenarioType, Verdict, InputJson, ResultJson, CreatedAt
              FROM Analytics.Scenarios WHERE UserId=@UserId AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return rows.ToList();
    }

    public async Task<bool> SoftDeleteAsync(long id, long userId, CancellationToken ct = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "UPDATE Analytics.Scenarios SET DeletedAt=SYSUTCDATETIME() WHERE Id=@Id AND UserId=@UserId AND DeletedAt IS NULL",
            new { Id = id, UserId = userId }) == 1;
    }
}
