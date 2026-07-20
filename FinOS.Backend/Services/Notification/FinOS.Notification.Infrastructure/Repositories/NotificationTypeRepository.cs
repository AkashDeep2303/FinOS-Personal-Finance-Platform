using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Notification.Domain.Entities;
using FinOS.Notification.Domain.Interfaces;

namespace FinOS.Notification.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of INotificationTypeRepository.
/// Note: NotificationType uses int keys, so this does NOT extend the generic IRepository{T}.
/// </summary>
public class NotificationTypeRepository : INotificationTypeRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public NotificationTypeRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<NotificationType?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<NotificationType>(
            "SELECT * FROM Notification.NotificationTypes WHERE Id = @Id", new { Id = id });
    }

    public async Task<List<NotificationType>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<NotificationType>(
            "SELECT * FROM Notification.NotificationTypes ORDER BY Category, Name");
        return result.ToList();
    }

    public async Task<List<NotificationType>> GetEnabledAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<NotificationType>(
            "SELECT * FROM Notification.NotificationTypes WHERE IsEnabled = 1 ORDER BY Category, Name");
        return result.ToList();
    }

    public async Task<NotificationType> AddAsync(NotificationType entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO Notification.NotificationTypes (Name, Description, Category, IsEnabled)
            VALUES (@Name, @Description, @Category, @IsEnabled);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        var id = await connection.ExecuteScalarAsync<int>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(NotificationType entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            @"UPDATE Notification.NotificationTypes
              SET Name = @Name, Description = @Description, Category = @Category, IsEnabled = @IsEnabled
              WHERE Id = @Id", entity);
    }

    public async Task RemoveAsync(NotificationType entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            "DELETE FROM Notification.NotificationTypes WHERE Id = @Id", new { entity.Id
    });
    }
}
