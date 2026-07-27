using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Application.Interfaces;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public class LoginCommand : IRequest<AuthResponse>
{
    public LoginRequest Request { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ITotpValidator _totpValidator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ITotpValidator totpValidator,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _totpValidator = totpValidator;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // Get user by email
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user == null)
        {
            throw new DomainException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new DomainException("ACCOUNT_DISABLED", "This account has been disabled. Please contact support.");
        }

        // Check if user is locked out
        if (user.IsLockedOut)
        {
            var remainingTime = user.LockoutEnd!.Value - DateTime.UtcNow;
            throw new DomainException("ACCOUNT_LOCKED",
                $"Account is locked. Please try again in {remainingTime.Minutes} minutes.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed access count
            var failedCount = await _userRepository.IncrementAccessFailedCountAsync(user.Id, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Lock out if exceeded max attempts
            if (failedCount >= MaxFailedAttempts && user.LockoutEnabled)
            {
                await _userRepository.LockUserAsync(user.Id, DefaultLockoutDuration, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                throw new DomainException("ACCOUNT_LOCKED",
                    $"Account has been locked due to too many failed attempts. Please try again in {DefaultLockoutDuration.Minutes} minutes.");
            }

            throw new DomainException("INVALID_CREDENTIALS",
                $"Invalid email or password. {MaxFailedAttempts - failedCount} attempt(s) remaining.");
        }

        // Check 2FA if enabled
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    TwoFactorRequired = true
                };
            }

            if (string.IsNullOrWhiteSpace(user.TwoFactorSecret) ||
                !_totpValidator.Validate(user.TwoFactorSecret, request.TwoFactorCode, DateTime.UtcNow))
            {
                throw new DomainException("INVALID_2FA_CODE", "Invalid two-factor authentication code.");
            }
        }

        // Reset failed access count on successful login
        await _userRepository.ResetAccessFailedCountAsync(user.Id, ct);

        // Update last login
        await _userRepository.UpdateLastLoginAsync(user.Id, command.IpAddress, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Generate tokens
        var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        // Revoke existing refresh tokens
        // (optional: revoke all previous tokens, or keep them active for multi-device)

        // Save new refresh token
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            JwtId = accessToken.JwtId,
            ExpiresAt = _tokenService.GetRefreshTokenExpiry(),
            IsRevoked = false,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, command.IpAddress, ct);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AccessToken = accessToken.Token,
            RefreshToken = refreshTokenString,
            AccessTokenExpiry = accessToken.ExpiresAt,
            RefreshTokenExpiry = refreshToken.ExpiresAt,
            Roles = roles,
            TwoFactorRequired = false
        };
    }
}
