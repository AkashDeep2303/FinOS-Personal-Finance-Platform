using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.AIAssistant.Domain.Entities;
using FinOS.AIAssistant.Domain.Enums;
using FinOS.AIAssistant.Domain.Interfaces;

namespace FinOS.AIAssistant.Infrastructure.Repositories;

public class AIMessageRepository : IAIMessageRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public AIMessageRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AIMessage?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AIMessage>(
            "SELECT * FROM [AI].[AIMessages] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<AIMessage>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<AIMessage>(dataSql, dp);

        return new PagedResult<AIMessage>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}", param);
    }

    public async Task<List<AIMessage>> GetByConversationIdAsync(long conversationId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<AIMessage>(
            "SELECT * FROM [AI].[AIMessages] WHERE ConversationId = @ConversationId ORDER BY CreatedAt ASC",
            new { ConversationId = conversationId });
        return result.ToList();
    }

    public async Task<List<AIMessage>> GetRecentByUserAsync(long userId, int count, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT TOP (@Count) m.* 
            FROM [AI].[AIMessages] m
            INNER JOIN [AI].[AIConversations] c ON m.ConversationId = c.Id
            WHERE c.UserId = @UserId
            ORDER BY m.CreatedAt DESC";

        var result = await connection.QueryAsync<AIMessage>(sql, new { UserId = userId, Count = count });
        return result.ToList();
    }

    public async Task<AIMessage> CreateAsync(AIMessage message, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO [AI].[AIMessages] (ConversationId, Role, Content, TokenCount, CreatedAt)
            VALUES (@ConversationId, @Role, @Content, @TokenCount, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            message.ConversationId,
            Role = message.Role.ToString(),
            message.Content,
            message.TokenCount,
            CreatedAt = DateTime.UtcNow
        });

        message.Id = id;
        return message;
    }

    public async Task UpdateAsync(AIMessage message, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE [AI].[AIMessages]
            SET FeedbackRating = @FeedbackRating,
                FeedbackComment = @FeedbackComment
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            message.Id,
            message.FeedbackRating,
            message.FeedbackComment
        });
    }

    public Task<AIMessage> AddAsync(AIMessage entity, CancellationToken ct = default)
    {
        return CreateAsync(entity, ct);
    }


    public Task RemoveAsync(AIMessage entity, CancellationToken ct = default) => Task.CompletedTask;
}
