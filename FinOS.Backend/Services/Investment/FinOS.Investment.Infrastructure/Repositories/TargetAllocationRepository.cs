using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class TargetAllocationRepository : ITargetAllocationRepository
{
    private readonly IConnectionFactory _connectionFactory;
    public TargetAllocationRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyDictionary<string, decimal>> GetAsync(long portfolioId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<(string AssetClass, decimal TargetPct)>(
            new CommandDefinition(
                "SELECT AssetClass, TargetPct FROM Investment.PortfolioTargetAllocations WHERE PortfolioId = @PortfolioId",
                new { PortfolioId = portfolioId }, cancellationToken: ct));
        return rows.ToDictionary(x => x.AssetClass, x => x.TargetPct, StringComparer.OrdinalIgnoreCase);
    }

    public async Task ReplaceAsync(long portfolioId, IReadOnlyDictionary<string, decimal> targets, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Investment.PortfolioTargetAllocations WHERE PortfolioId = @PortfolioId",
            new { PortfolioId = portfolioId }, transaction, cancellationToken: ct));
        foreach (var target in targets.Where(x => x.Value > 0))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO Investment.PortfolioTargetAllocations (PortfolioId, AssetClass, TargetPct)
                  VALUES (@PortfolioId, @AssetClass, @TargetPct)",
                new { PortfolioId = portfolioId, AssetClass = target.Key, TargetPct = target.Value },
                transaction, cancellationToken: ct));
        }
        transaction.Commit();
    }
}
