using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Application.Interfaces;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public RefreshTokenRequest Request { get; set; } = null!;
    public string? IpAddress { get; set; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // Get the refresh token
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

        if (existingRefreshToken == null)
        {
            throw new DomainException("INVALID_TOKEN", "Invalid refresh token.");
        }

        // Validate the refresh token
        if (existingRefreshToken.IsRevoked)
        {
            // Token has been revoked - this could indicate token reuse/theft
            // Revoke all descendant tokens for security
            await _refreshTokenRepository.RevokeAllByUserIdAsync(existingRefreshToken.UserId, null, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            throw new DomainException("TOKEN_REVOKED", "Refresh token has been revoked. Please login again.");
        }

        if (existingRefreshToken.IsUsed)
        {
            throw new DomainException("TOKEN_USED", "Refresh token has already been used.");
        }

        if (existingRefreshToken.IsExpired)
        {
            throw new DomainException("TOKEN_EXPIRED", "Refresh token has expired. Please login again.");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(existingRefreshToken.UserId, ct);
        if (user == null || !user.IsActive)
        {
            throw new DomainException("INVALID_USER", "User account not found or disabled.");
        }

        // Mark current refresh token as used
        existingRefreshToken.IsUsed = true;
        existingRefreshToken.ReplacedByToken = null;

        // Generate new tokens
        var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshTokenString = _tokenService.GenerateRefreshToken();

        // Create new refresh token
        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            JwtId = accessToken.JwtId,
            ExpiresAt = _tokenService.GetRefreshTokenExpiry(),
            IsRevoked = false,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ReplacedByToken = null
        };

        // Update old token's replaced-by reference
        existingRefreshToken.ReplacedByToken = newRefreshTokenString;

        // Add new refresh token
        user.RefreshTokens.Add(newRefreshToken);

        await _refreshTokenRepository.UpdateAsync(existingRefreshToken);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AccessToken = accessToken.Token,
            RefreshToken = newRefreshTokenString,
            AccessTokenExpiry = accessToken.ExpiresAt,
            RefreshTokenExpiry = newRefreshToken.ExpiresAt,
            Roles = roles,
            TwoFactorRequired = false
        };
    }
}
