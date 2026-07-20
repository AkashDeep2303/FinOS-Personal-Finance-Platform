using System.Data;
using Dapper;
using FinOS.Common.Interfaces;
using FinOS.Common.Models;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;

namespace FinOS.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public UserRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Security.Users WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = id });
        if (user is not null) await LoadUserRelationsAsync(connection, user);
        return user;
    }

    public async Task<PagedResult<User>> PagedAsync(PagedQuery query, string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
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
        var items = (await connection.QueryAsync<User>(dataSql, param)).ToList();
        foreach (var user in items) await LoadUserRolesAsync(connection, user);

        return new PagedResult<User> { Items = items, TotalCount = totalCount, Page = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<long> CountAsync(string schema, string tableName, string whereClause = "", object? param = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = string.IsNullOrWhiteSpace(whereClause) ? "WHERE DeletedAt IS NULL" : $"WHERE DeletedAt IS NULL AND ({whereClause})";
        return await connection.ExecuteScalarAsync<long>($"SELECT COUNT(1) FROM [{schema}].[{tableName}] {where}", param);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Security.Users WHERE LOWER(Email) = @Email AND DeletedAt IS NULL",
            new { Email = normalizedEmail });
        if (user is not null) await LoadUserRelationsAsync(connection, user);
        return user;
    }

    public async Task<User?> GetByOAuthProviderAsync(string provider, string providerId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Security.Users WHERE OAuthProvider = @OAuthProvider AND OAuthProviderId = @OAuthProviderId AND DeletedAt IS NULL",
            new { OAuthProvider = provider, OAuthProviderId = providerId });
        if (user is not null) await LoadUserRelationsAsync(connection, user);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Security.Users WHERE LOWER(Email) = @Email AND DeletedAt IS NULL",
            new { Email = normalizedEmail });
        return count > 0;
    }

    public async Task<User> CreateAsync(User user, int roleId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Email", user.Email, DbType.String, size: 256);
        parameters.Add("@PasswordHash", user.PasswordHash, DbType.String, size: 500);
        parameters.Add("@PasswordSalt", user.PasswordSalt, DbType.String, size: 200);
        parameters.Add("@FirstName", user.FirstName, DbType.String, size: 50);
        parameters.Add("@LastName", user.LastName, DbType.String, size: 50);
        parameters.Add("@PhoneNumber", user.PhoneNumber, DbType.String, size: 20);
        parameters.Add("@ProfileImageUrl", user.ProfileImageUrl, DbType.String, size: 500);
        parameters.Add("@Currency", user.Currency, DbType.String, size: 3);
        parameters.Add("@TimeZone", user.TimeZone, DbType.String, size: 50);
        parameters.Add("@Locale", user.Locale, DbType.String, size: 10);
        parameters.Add("@OAuthProvider", user.OAuthProvider, DbType.String, size: 50);
        parameters.Add("@OAuthProviderId", user.OAuthProviderId, DbType.String, size: 256);
        parameters.Add("@RoleId", roleId, DbType.Int32);
        parameters.Add("@NewUserId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("Security.sp_CreateUser", parameters, commandType: CommandType.StoredProcedure);
        user.Id = parameters.Get<long>("@NewUserId");
        await LoadUserRolesAsync(connection, user);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", user.Id, DbType.Int64);
        parameters.Add("@FirstName", user.FirstName, DbType.String, size: 50);
        parameters.Add("@LastName", user.LastName, DbType.String, size: 50);
        parameters.Add("@PhoneNumber", user.PhoneNumber, DbType.String, size: 20);
        parameters.Add("@ProfileImageUrl", user.ProfileImageUrl, DbType.String, size: 500);
        parameters.Add("@Currency", user.Currency, DbType.String, size: 3);
        parameters.Add("@TimeZone", user.TimeZone, DbType.String, size: 50);
        parameters.Add("@Locale", user.Locale, DbType.String, size: 10);
        parameters.Add("@TwoFactorEnabled", user.TwoFactorEnabled, DbType.Boolean);
        parameters.Add("@TwoFactorSecret", user.TwoFactorSecret, DbType.String, size: 500);
        parameters.Add("@IsActive", user.IsActive, DbType.Boolean);
        parameters.Add("@PhoneVerified", user.PhoneVerified, DbType.Boolean);

        await connection.ExecuteAsync("Security.sp_UpdateUser", parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task ChangePasswordAsync(long userId, string oldPasswordHash, string newPasswordHash, string newPasswordSalt, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int64);
        parameters.Add("@OldPasswordHash", oldPasswordHash, DbType.String, size: 500);
        parameters.Add("@NewPasswordHash", newPasswordHash, DbType.String, size: 500);
        parameters.Add("@NewPasswordSalt", newPasswordSalt, DbType.String, size: 200);
        await connection.ExecuteAsync("Security.sp_ChangePassword", parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateLastLoginAsync(long userId, string? ipAddress, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            "UPDATE Security.Users SET LastLoginAt = @LastLoginAt, UpdatedAt = @UpdatedAt WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = userId, LastLoginAt = now, UpdatedAt = now });
    }

    public async Task<int> IncrementAccessFailedCountAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            "UPDATE Security.Users SET AccessFailedCount = AccessFailedCount + 1, UpdatedAt = @UpdatedAt WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = userId, UpdatedAt = now });
        return await connection.ExecuteScalarAsync<int>("SELECT AccessFailedCount FROM Security.Users WHERE Id = @Id", new { Id = userId });
    }

    public async Task ResetAccessFailedCountAsync(long userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            "UPDATE Security.Users SET AccessFailedCount = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = userId, UpdatedAt = now });
    }

    public async Task LockUserAsync(long userId, TimeSpan lockoutDuration, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var now = DateTime.UtcNow;
        var lockoutEnd = now.Add(lockoutDuration);
        await connection.ExecuteAsync(
            "UPDATE Security.Users SET LockoutEnd = @LockoutEnd, UpdatedAt = @UpdatedAt WHERE Id = @Id AND DeletedAt IS NULL",
            new { Id = userId, LockoutEnd = lockoutEnd, UpdatedAt = now });
    }

    private static async Task LoadUserRelationsAsync(Microsoft.Data.SqlClient.SqlConnection connection, User user)
    {
        await LoadUserRolesAsync(connection, user);
        var refreshTokens = await connection.QueryAsync<RefreshToken>(
            "SELECT * FROM Security.RefreshTokens WHERE UserId = @UserId", new { UserId = user.Id });
        user.RefreshTokens = refreshTokens.ToList();
    }

    private static async Task LoadUserRolesAsync(Microsoft.Data.SqlClient.SqlConnection connection, User user)
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
    }

    public async Task<User> AddAsync(User entity, CancellationToken ct = default)
    {
        return await CreateAsync(entity, 2, ct);
    }


    public Task RemoveAsync(User entity, CancellationToken ct = default) => Task.CompletedTask;
}
