using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;

namespace FinOS.Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
            "SELECT * FROM Security.RefreshTokens WHERE Id = @Id", new { Id = id });
    }

    public async Task<PagedResult<RefreshToken>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
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
        var items = (await connection.QueryAsync<RefreshToken>(dataSql, param)).ToList();

        return new PagedResult<RefreshToken> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "" : $"WHERE {whereClause}";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var refreshToken = await connection.QueryFirstOrDefaultAsync<RefreshToken>(
            "SELECT * FROM Security.RefreshTokens WHERE Token = @Token", new { Token = token });

        if (refreshToken is not null)
        {
            var user = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL",
                new { UserId = refreshToken.UserId });

            if (user is not null)
            {
                var userRoles = await connection.QueryAsync<UserRole, Role, UserRole>(
                    """
                    SELECT ur.UserId, ur.RoleId, ur.AssignedAt,
                           r.Id, r.Name, r.Description, r.CreatedAt
                    FROM Security.UserRoles ur
                    INNER JOIN Security.Roles r ON ur.RoleId = r.Id
                    WHERE ur.UserId = @UserId
                    """,
                    (userRole, role) => { userRole.Role = role; return userRole; },
                    new { UserId = user.Id }, splitOn: "Id");
                user.UserRoles = userRoles.ToList();
                refreshToken.User = user;
            }
        }
        return refreshToken;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken token, string? ipAddress, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", token.UserId, DbType.Int64);
        parameters.Add("@Token", token.Token, DbType.String, size: 256);
        parameters.Add("@JwtId", token.JwtId, DbType.String, size: 100);
        parameters.Add("@ExpiresAt", token.ExpiresAt, DbType.DateTime2);
        parameters.Add("@IpAddress", ipAddress, DbType.String, size: 50);
        parameters.Add("@NewTokenId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("Security.sp_CreateRefreshToken", parameters, commandType: CommandType.StoredProcedure);
        token.Id = parameters.Get<long>("@NewTokenId");
        return token;
    }

    public async Task RevokeAsync(string token, string revokedByIp, string? replacedByToken, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Token", token, DbType.String, size: 256);
        parameters.Add("@RevokedByIp", revokedByIp, DbType.String, size: 50);
        parameters.Add("@ReplacedByToken", replacedByToken, DbType.String, size: 256);
        await connection.ExecuteAsync("Security.sp_RevokeRefreshToken", parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task RevokeAllByUserIdAsync(long userId, string? replacedByToken, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            """
            UPDATE Security.RefreshTokens
            SET IsRevoked = 1, RevokedAt = @RevokedAt, ReplacedByToken = @ReplacedByToken
            WHERE UserId = @UserId AND IsRevoked = 0
            """,
            new { UserId = userId, RevokedAt = now, ReplacedByToken = replacedByToken });
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tokens = await connection.QueryAsync<RefreshToken>(new CommandDefinition(
            """
            SELECT Id, UserId, JwtId, IsRevoked, IsUsed, ExpiresAt, CreatedAt, RevokedAt
            FROM Security.RefreshTokens
            WHERE UserId = @UserId
              AND IsRevoked = 0
              AND IsUsed = 0
              AND ExpiresAt > SYSUTCDATETIME()
            ORDER BY CreatedAt DESC;
            """,
            new { UserId = userId }, cancellationToken: ct));
        return tokens.AsList();
    }

    public async Task RevokeByIdAsync(
        long userId, long tokenId, string? revokedByIp, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE Security.RefreshTokens
            SET IsRevoked = 1,
                RevokedAt = SYSUTCDATETIME(),
                RevokedByIp = @RevokedByIp
            WHERE Id = @TokenId
              AND UserId = @UserId
              AND IsRevoked = 0;
            """,
            new { UserId = userId, TokenId = tokenId, RevokedByIp = revokedByIp },
            cancellationToken: ct));
    }

    public async Task RevokeAllExceptJwtIdAsync(
        long userId, string currentJwtId, string? revokedByIp, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE Security.RefreshTokens
            SET IsRevoked = 1,
                RevokedAt = SYSUTCDATETIME(),
                RevokedByIp = @RevokedByIp
            WHERE UserId = @UserId
              AND JwtId <> @CurrentJwtId
              AND IsRevoked = 0
              AND IsUsed = 0;
            """,
            new { UserId = userId, CurrentJwtId = currentJwtId, RevokedByIp = revokedByIp },
            cancellationToken: ct));
    }

    public async Task MarkAsUsedAsync(long tokenId, string? replacedByToken, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Security.RefreshTokens SET IsUsed = 1, ReplacedByToken = @ReplacedByToken WHERE Id = @Id",
            new { Id = tokenId, ReplacedByToken = replacedByToken });
    }

    public async Task<RefreshToken> RotateAsync(
        long existingTokenId, RefreshToken replacement, string? ipAddress, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var affected = await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE Security.RefreshTokens
                SET IsUsed = 1, ReplacedByToken = @ReplacementToken
                WHERE Id = @ExistingTokenId AND IsUsed = 0 AND IsRevoked = 0;
                """,
                new { ExistingTokenId = existingTokenId, ReplacementToken = replacement.Token },
                transaction, cancellationToken: ct));
            if (affected != 1)
                throw new InvalidOperationException("Refresh token is no longer active.");

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", replacement.UserId, DbType.Int64);
            parameters.Add("@Token", replacement.Token, DbType.String, size: 256);
            parameters.Add("@JwtId", replacement.JwtId, DbType.String, size: 100);
            parameters.Add("@ExpiresAt", replacement.ExpiresAt, DbType.DateTime2);
            parameters.Add("@IpAddress", ipAddress, DbType.String, size: 50);
            parameters.Add("@NewTokenId", dbType: DbType.Int64, direction: ParameterDirection.Output);
            await connection.ExecuteAsync(new CommandDefinition(
                "Security.sp_CreateRefreshToken", parameters, transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
            replacement.Id = parameters.Get<long>("@NewTokenId");
            await transaction.CommitAsync(ct);
            return replacement;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> CleanExpiredTokensAsync(int olderThanDays, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@OlderThanDays", olderThanDays, DbType.Int32);
        return await connection.ExecuteAsync("Security.sp_CleanExpiredTokens", parameters, commandType: CommandType.StoredProcedure);
    }

    public Task<RefreshToken> AddAsync(RefreshToken entity, CancellationToken ct = default)
    {
        return CreateAsync(entity, null, ct);
    }

    public Task UpdateAsync(RefreshToken entity, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(RefreshToken entity, CancellationToken ct = default) => Task.CompletedTask;
}
