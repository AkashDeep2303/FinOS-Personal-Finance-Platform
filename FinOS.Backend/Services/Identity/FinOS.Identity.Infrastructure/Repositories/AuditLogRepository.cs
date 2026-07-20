using System.Data;
using System.Text;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;

namespace FinOS.Identity.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public AuditLogRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuditLog?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AuditLog>(
            "SELECT * FROM Security.AuditLogs WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<AuditLog>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        var sortDirection = query.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var sortColumn = !string.IsNullOrWhiteSpace(query.SortBy) ? query.SortBy : "CreatedAt";
        var offset = (query.PageNumber - 1) * query.PageSize;

        var countSql = $"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}";
        var dataSql = $"""
            SELECT * FROM [{schema}].[{tableName}] {where}
            ORDER BY [{sortColumn}] {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {query.PageSize} ROWS ONLY
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = (await connection.QueryAsync<AuditLog>(dataSql, param)).ToList();

        return new PagedResult<AuditLog> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", auditLog.UserId, DbType.Int64);
        parameters.Add("@ActionType", auditLog.ActionType, DbType.String, size: 50);
        parameters.Add("@EntityType", auditLog.EntityType, DbType.String, size: 100);
        parameters.Add("@EntityId", auditLog.EntityId, DbType.String, size: 50);
        parameters.Add("@OldValues", auditLog.OldValues, DbType.String, size: 4000);
        parameters.Add("@NewValues", auditLog.NewValues, DbType.String, size: 4000);
        parameters.Add("@IpAddress", auditLog.IpAddress, DbType.String, size: 50);
        parameters.Add("@UserAgent", auditLog.UserAgent, DbType.String, size: 500);

        await connection.ExecuteAsync("Security.sp_AddAuditLog", parameters, commandType: CommandType.StoredProcedure);

        var inserted = await connection.QueryFirstOrDefaultAsync<AuditLog>(
            """
            SELECT TOP 1 * FROM Security.AuditLogs
            WHERE UserId = @UserId AND ActionType = @ActionType AND EntityType = @EntityType
            ORDER BY Id DESC
            """,
            new { UserId = auditLog.UserId, ActionType = auditLog.ActionType, EntityType = auditLog.EntityType });

        if (inserted is not null)
        {
            auditLog.Id = inserted.Id;
            auditLog.CreatedAt = inserted.CreatedAt;
        }
        return auditLog;
    }

    public async Task<PagedResult<AuditLog>> GetFilteredAsync(long? userId, string? actionType, string? entityType, DateTime? fromDate, DateTime? toDate, string? searchTerm, PagedQuery pagination, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var whereBuilder = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            whereBuilder.Append(" AND UserId = @UserId");
            parameters.Add("@UserId", userId.Value, DbType.Int64);
        }
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            whereBuilder.Append(" AND ActionType = @ActionType");
            parameters.Add("@ActionType", actionType, DbType.String, size: 50);
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            whereBuilder.Append(" AND EntityType = @EntityType");
            parameters.Add("@EntityType", entityType, DbType.String, size: 100);
        }
        if (fromDate.HasValue)
        {
            whereBuilder.Append(" AND CreatedAt >= @FromDate");
            parameters.Add("@FromDate", fromDate.Value, DbType.DateTime2);
        }
        if (toDate.HasValue)
        {
            whereBuilder.Append(" AND CreatedAt <= @ToDate");
            parameters.Add("@ToDate", toDate.Value, DbType.DateTime2);
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereBuilder.Append(" AND (LOWER(ActionType) LIKE @SearchTerm OR LOWER(EntityType) LIKE @SearchTerm OR LOWER(EntityId) LIKE @SearchTerm)");
            parameters.Add("@SearchTerm", $"%{searchTerm.ToLower()}%", DbType.String);
        }

        var whereClause = whereBuilder.ToString();
        var countSql = $"SELECT COUNT(1) FROM Security.AuditLogs {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var sortDirection = pagination.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
        var offset = (pagination.PageNumber - 1) * pagination.PageSize;
        var dataSql = $"""
            SELECT * FROM Security.AuditLogs {whereClause}
            ORDER BY CreatedAt {sortDirection}
            OFFSET {offset} ROWS FETCH NEXT {pagination.PageSize} ROWS ONLY
            """;

        var items = (await connection.QueryAsync<AuditLog>(dataSql, parameters)).ToList();

        return new PagedResult<AuditLog> { Items = items, TotalCount = totalCount, Page = pagination.PageNumber, PageSize = pagination.PageSize };
    }

    public Task UpdateAsync(AuditLog entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(AuditLog entity, CancellationToken ct = default) => Task.CompletedTask;
}
