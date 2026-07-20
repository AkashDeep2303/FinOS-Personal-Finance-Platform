using FinOS.Common.Interfaces;

namespace FinOS.Identity.Domain.Interfaces;

public interface IUserRepository : IRepository<Domain.Entities.User>
{
    Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Domain.Entities.User?> GetByOAuthProviderAsync(string provider, string providerId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<Domain.Entities.User> CreateAsync(Domain.Entities.User user, int roleId, CancellationToken ct = default);
    Task UpdateAsync(Domain.Entities.User user, CancellationToken ct = default);
    Task ChangePasswordAsync(long userId, string oldPasswordHash, string newPasswordHash, string newPasswordSalt, CancellationToken ct = default);
    Task UpdateLastLoginAsync(long userId, string? ipAddress, CancellationToken ct = default);
    Task<int> IncrementAccessFailedCountAsync(long userId, CancellationToken ct = default);
    Task ResetAccessFailedCountAsync(long userId, CancellationToken ct = default);
    Task LockUserAsync(long userId, TimeSpan lockoutDuration, CancellationToken ct = default);
}
