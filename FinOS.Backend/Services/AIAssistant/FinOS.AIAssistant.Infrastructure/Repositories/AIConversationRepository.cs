using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.AIAssistant.Domain.Entities;
using FinOS.AIAssistant.Domain.Interfaces;

namespace FinOS.AIAssistant.Infrastructure.Repositories;

public class AIConversationRepository : IAIConversationRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public AIConversationRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AIConversation?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AIConversation>(
            "SELECT * FROM [AI].[AIConversations] WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<AIConversation>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $" WHERE {whereClause}";
        var countSql = $"SELECT COUNT(*) FROM [{schema}].[{tableName}]{where}";
        var dataSql = $"SELECT * FROM [{schema}].[{tableName}]{where} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var dp = new DynamicParameters(param);
        dp.Add("@Offset", (query.PageNumber - 1) * query.PageSize);
        dp.Add("@PageSize", query.PageSize);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param);
        var items = await connection.QueryAsync<AIConversation>(dataSql, dp);

        return new PagedResult<AIConversation>
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

    public async Task<List<AIConversation>> GetByUserIdAsync(long userId, int count, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<AIConversation>(
            @"SELECT TOP (@Count) * FROM [AI].[AIConversations] 
              WHERE UserId = @UserId 
              ORDER BY UpdatedAt DESC",
            new { UserId = userId, Count = count });
        return result.ToList();
    }

    public async Task<AIConversation?> GetWithMessagesAsync(long conversationId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT * FROM [AI].[AIConversations] WHERE Id = @ConversationId;
            SELECT * FROM [AI].[AIMessages] WHERE ConversationId = @ConversationId ORDER BY CreatedAt ASC;";

        using var multi = await connection.QueryMultipleAsync(sql, new { ConversationId = conversationId });
        var conversation = await multi.ReadFirstOrDefaultAsync<AIConversation>();
        if (conversation != null)
        {
            var messages = (await multi.ReadAsync<AIMessage>()).ToList();
            conversation.Messages = messages;
        }
        return conversation;
    }

    public async Task<AIConversation> CreateAsync(AIConversation conversation, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO [AI].[AIConversations] (UserId, Title, QueryType, CreatedAt, UpdatedAt)
            VALUES (@UserId, @Title, @QueryType, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            conversation.UserId,
            conversation.Title,
            QueryType = conversation.QueryType?.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        conversation.Id = id;
        return conversation;
    }

    public async Task UpdateAsync(AIConversation conversation, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE [AI].[AIConversations]
            SET Title = @Title,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            conversation.Id,
            conversation.Title,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Task<AIConversation> AddAsync(AIConversation entity, CancellationToken ct = default)
    {
        return CreateAsync(entity, ct);
    }


    public Task RemoveAsync(AIConversation entity, CancellationToken ct = default) => Task.CompletedTask;
}
