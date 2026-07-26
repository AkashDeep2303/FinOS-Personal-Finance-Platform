using FinOS.Common.Interfaces;

namespace FinOS.Identity.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<Domain.Entities.RefreshToken>
{
    Task<Domain.Entities.RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Domain.Entities.RefreshToken> CreateAsync(Domain.Entities.RefreshToken token, string? ipAddress, CancellationToken ct = default);
    Task RevokeAsync(string token, string revokedByIp, string? replacedByToken, CancellationToken ct = default);
    Task RevokeAllByUserIdAsync(long userId, string? replacedByToken, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Entities.RefreshToken>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
    Task RevokeByIdAsync(long userId, long tokenId, string? revokedByIp, CancellationToken ct = default);
    Task RevokeAllExceptJwtIdAsync(long userId, string currentJwtId, string? revokedByIp, CancellationToken ct = default);
    Task MarkAsUsedAsync(long tokenId, string? replacedByToken, CancellationToken ct = default);
    Task<Domain.Entities.RefreshToken> RotateAsync(long existingTokenId, Domain.Entities.RefreshToken replacement, string? ipAddress, CancellationToken ct = default);
    Task<int> CleanExpiredTokensAsync(int olderThanDays, CancellationToken ct = default);
}
