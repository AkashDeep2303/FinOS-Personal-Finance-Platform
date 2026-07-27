using Dapper;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;

namespace FinOS.CoreFinance.Infrastructure.Repositories;

public sealed class DataSourceRepository(IConnectionFactory connectionFactory) : IDataSourceRepository
{
    public async Task<IReadOnlyList<DataSource>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, SourceType, DisplayName, InstitutionName,
                   ConnectionMode, Status, LastImportedAt, CreatedAt, UpdatedAt
            FROM Core.DataSources
            WHERE UserId = @UserId AND DeletedAt IS NULL
            ORDER BY Status, DisplayName;
            """;
        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<DataSource>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<DataSource> AddAsync(DataSource source, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT Core.DataSources
                (UserId, SourceType, DisplayName, InstitutionName, ConnectionMode, Status)
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.SourceType, INSERTED.DisplayName,
                   INSERTED.InstitutionName, INSERTED.ConnectionMode, INSERTED.Status,
                   INSERTED.LastImportedAt, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES
                (@UserId, @SourceType, @DisplayName, @InstitutionName, N'ManualImport', N'Active');
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<DataSource>(
            new CommandDefinition(sql, source, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Core.DataSources
            SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME(), Status = N'Paused'
            WHERE Id = @Id AND UserId = @UserId AND DeletedAt IS NULL;
            """;
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken)) == 1;
    }
}
