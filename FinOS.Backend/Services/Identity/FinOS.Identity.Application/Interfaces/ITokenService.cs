using FinOS.Identity.Application.DTOs;

namespace FinOS.Identity.Application.Interfaces;

public interface ITokenService
{
    TokenResult GenerateAccessToken(Domain.Entities.User user, List<string> roles);
    string GenerateRefreshToken();
    string GenerateJwtId();
    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
    long? GetUserIdFromToken(string token);
    string? GetJwtIdFromToken(string token);
}

public class TokenResult
{
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
