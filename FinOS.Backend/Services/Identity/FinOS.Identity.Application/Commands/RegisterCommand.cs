using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Application.Interfaces;
using FinOS.Identity.Domain.Entities;
using FinOS.Identity.Domain.Interfaces;
using FinOS.EventBus.Events;
using FinOS.EventBus.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public class RegisterCommand : IRequest<AuthResponse>
{
    public RegisterRequest Request { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
        {
            throw new DomainException("EMAIL_EXISTS", $"A user with email '{request.Email}' already exists.");
        }

        // Create user entity
        var user = new User
        {
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            PasswordSalt = _passwordHasher.GenerateSalt(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Currency = request.Currency,
            TimeZone = request.TimeZone,
            Locale = request.Locale,
            IsActive = true,
            EmailVerified = false,
            PhoneVerified = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Assign default "User" role
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = 2, // Default "User" role - RoleId=1 is "Admin", RoleId=2 is "User"
            AssignedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);

        // Generate tokens
        var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "User").ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenString = _tokenService.GenerateRefreshToken();

        // Save refresh token
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

        user.RefreshTokens.Add(refreshToken);
        await _unitOfWork.SaveChangesAsync(ct);

        // Publish user registered event
        try
        {
            await _eventBus.PublishAsync(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }, ct);
        }
        catch
        {
            // Don't fail registration if event publishing fails
        }

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
