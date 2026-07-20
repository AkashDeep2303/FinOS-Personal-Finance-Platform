using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Notification.Domain.Entities;
using FinOS.Notification.Domain.Interfaces;

using NotificationEntity = FinOS.Notification.Domain.Entities.Notification;

namespace FinOS.Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public NotificationRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT n.*, nt.* FROM Notification.Notifications n
            LEFT JOIN Notification.NotificationTypes nt ON n.NotificationTypeId = nt.Id
            WHERE n.Id = @Id";
        var result = await connection.QueryAsync<NotificationEntity, NotificationType, NotificationEntity>(sql,
            (notification, notificationType) => { notification.NotificationType = notificationType; return notification; },
            new { Id = id }, splitOn: "Id");
        return result.FirstOrDefault();
    }

    public async Task<PagedResult<NotificationEntity>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var offset = (query.PageNumber - 1) * query.PageSize;
        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY CreatedAt DESC
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<NotificationEntity>(dataSql, param)).ToList();
        return new PagedResult<NotificationEntity> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<List<NotificationEntity>> GetUnreadByUserAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT n.*, nt.* FROM Notification.Notifications n
            LEFT JOIN Notification.NotificationTypes nt ON n.NotificationTypeId = nt.Id
            WHERE n.UserId = @UserId AND n.IsRead = 0
            ORDER BY n.CreatedAt DESC";
        var result = await connection.QueryAsync<NotificationEntity, NotificationType, NotificationEntity>(sql,
            (notification, notificationType) => { notification.NotificationType = notificationType; return notification; },
            new { UserId = userId }, splitOn: "Id");
        return result.ToList();
    }

    public async Task<PagedResult<NotificationEntity>> GetPagedByUserAsync(long userId, bool? isRead, int? notificationTypeId, PagedQuery query, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT n.*, nt.* FROM Notification.Notifications n
            LEFT JOIN Notification.NotificationTypes nt ON n.NotificationTypeId = nt.Id
            WHERE n.UserId = @UserId";

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        parameters.Add("@PageSize", query.PageSize);

        if (isRead.HasValue)
        {
            sql += " AND n.IsRead = @IsRead";
            parameters.Add("@IsRead", isRead.Value);
        }
        if (notificationTypeId.HasValue)
        {
            sql += " AND n.NotificationTypeId = @NotificationTypeId";
            parameters.Add("@NotificationTypeId", notificationTypeId.Value);
        }
        sql += " AND (n.ExpiresAt IS NULL OR n.ExpiresAt > @Now)";
        parameters.Add("@Now", DateTime.UtcNow);

        var countSql = sql.Replace("SELECT n.*, nt.*", "SELECT COUNT(1)");
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        sql += " ORDER BY n.CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        var items = (await connection.QueryAsync<NotificationEntity, NotificationType, NotificationEntity>(sql,
            (notification, notificationType) => { notification.NotificationType = notificationType; return notification; },
            parameters, splitOn: "Id")).ToList();

        return new PagedResult<NotificationEntity> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Notification.Notifications WHERE UserId = @UserId AND IsRead = 0 AND (ExpiresAt IS NULL OR ExpiresAt > @Now)",
            new { UserId = userId, Now = now });
    }

    public async Task MarkAsReadAsync(long notificationId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Notification.Notifications SET IsRead = 1, ReadAt = @ReadAt WHERE Id = @Id",
            new { Id = notificationId, ReadAt = DateTime.UtcNow });
    }

    public async Task MarkAllAsReadAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Notification.Notifications SET IsRead = 1, ReadAt = @ReadAt WHERE UserId = @UserId AND IsRead = 0",
            new { UserId = userId, ReadAt = DateTime.UtcNow });
    }

    public async Task<NotificationEntity> AddAsync(NotificationEntity entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO Notification.Notifications (UserId, NotificationTypeId, Title, Message, Priority, IsRead, ReadAt, ActionUrl, ExpiresAt, ScheduledAt, DeliveryStatus, CreatedAt, UpdatedAt)
            VALUES (@UserId, @NotificationTypeId, @Title, @Message, @Priority, @IsRead, @ReadAt, @ActionUrl, @ExpiresAt, @ScheduledAt, @DeliveryStatus, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        var id = await connection.ExecuteScalarAsync<long>(sql, entity);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            @"UPDATE Notification.Notifications
              SET NotificationTypeId = @NotificationTypeId, Title = @Title, Message = @Message,
                  IsRead = @IsRead, ReadAt = @ReadAt, IsActionTaken = @IsActionTaken,
                  ActionTakenAt = @ActionTakenAt, DeliveryChannel = @DeliveryChannel,
                  DeliveryStatus = @DeliveryStatus, SentAt = @SentAt, ExpiresAt = @ExpiresAt
              WHERE Id = @Id", entity);
    }

    public async Task RemoveAsync(NotificationEntity entity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            "DELETE FROM Notification.Notifications WHERE Id = @Id", new { entity.Id
    });
    }

    public async Task<List<NotificationEntity>> GetScheduledAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT * FROM Notification.Notifications 
                    WHERE Status = 'Scheduled' AND ScheduledAt <= @Now
                    ORDER BY ScheduledAt ASC";
        var result = await connection.QueryAsync<NotificationEntity>(sql, new { Now = DateTime.UtcNow });
        return result.ToList();
    }
}
