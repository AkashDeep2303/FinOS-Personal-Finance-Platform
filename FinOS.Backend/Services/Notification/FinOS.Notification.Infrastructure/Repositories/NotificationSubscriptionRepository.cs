using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Notification.Domain.Entities;
using FinOS.Notification.Domain.Interfaces;

namespace FinOS.Notification.Infrastructure.Repositories;

public class NotificationSubscriptionRepository : INotificationPreferenceRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public NotificationSubscriptionRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<NotificationPreference?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT np.*, nt.* FROM Notification.NotificationPreferences np
            LEFT JOIN Notification.NotificationTypes nt ON np.NotificationTypeId = nt.Id
            WHERE np.Id = @Id";
        var result = await connection.QueryAsync<NotificationPreference, NotificationType, NotificationPreference>(sql,
            (pref, notificationType) => { pref.NotificationType = notificationType; return pref; },
            new { Id = id }, splitOn: "Id");
        return result.FirstOrDefault();
    }

    public async Task<PagedResult<NotificationPreference>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var offset = (query.PageNumber - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY NotificationTypeId ASC
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<NotificationPreference>(dataSql, param)).ToList();
        return new PagedResult<NotificationPreference> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<NotificationPreference>> GetByUserAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT np.*, nt.* FROM Notification.NotificationPreferences np
            LEFT JOIN Notification.NotificationTypes nt ON np.NotificationTypeId = nt.Id
            WHERE np.UserId = @UserId
            ORDER BY np.NotificationTypeId";
        var result = await connection.QueryAsync<NotificationPreference, NotificationType, NotificationPreference>(sql,
            (pref, notificationType) => { pref.NotificationType = notificationType; return pref; },
            new { UserId = userId }, splitOn: "Id");
        return result.ToList();
    }

    public async Task<NotificationPreference?> GetByUserAndTypeAsync(long userId, int notificationTypeId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT np.*, nt.* FROM Notification.NotificationPreferences np
            LEFT JOIN Notification.NotificationTypes nt ON np.NotificationTypeId = nt.Id
            WHERE np.UserId = @UserId AND np.NotificationTypeId = @NotificationTypeId";
        var result = await connection.QueryAsync<NotificationPreference, NotificationType, NotificationPreference>(sql,
            (pref, notificationType) => { pref.NotificationType = notificationType; return pref; },
            new { UserId = userId, NotificationTypeId = notificationTypeId }, splitOn: "Id");
        return result.FirstOrDefault();
    }

    public async Task<NotificationPreference> AddAsync(NotificationPreference entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO Notification.NotificationPreferences (UserId, NotificationTypeId, IsEnabled, EmailEnabled, PushEnabled, SmsEnabled, InAppEnabled, QuietHoursStart, QuietHoursEnd, CreatedAt, UpdatedAt)
            VALUES (@UserId, @NotificationTypeId, @IsEnabled, @EmailEnabled, @PushEnabled, @SmsEnabled, @InAppEnabled, @QuietHoursStart, @QuietHoursEnd, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(NotificationPreference entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            @"UPDATE Notification.NotificationPreferences
              SET IsEnabled = 1, EmailEnabled = @EmailEnabled, PushEnabled = @PushEnabled,
                  SmsEnabled = @SmsEnabled, InAppEnabled = @InAppEnabled,
                  QuietHoursStart = @QuietHoursStart, QuietHoursEnd = @QuietHoursEnd, UpdatedAt = @UpdatedAt
              WHERE Id = @Id", entity);
    }

    public async Task RemoveAsync(NotificationPreference entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            "DELETE FROM Notification.NotificationPreferences WHERE Id = @Id", new { entity.Id
    });
    }
}
