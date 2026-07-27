using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Investment.Domain.Entities;
using FinOS.Investment.Domain.Interfaces;

namespace FinOS.Investment.Infrastructure.Repositories;

public class InvestmentTypeRepository : IInvestmentTypeRepository
{
    private readonly IConnectionFactory _connectionFactory;
    public InvestmentTypeRepository(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<InvestmentType>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<InvestmentType>(
            "SELECT Id, Name, AssetClass, Icon, IsTaxSaving, SortOrder FROM Investment.InvestmentTypes ORDER BY SortOrder");
        return rows.ToList();
    }
}
