using FinOS.Common.Exceptions;
using FinOS.Identity.Application.DTOs;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Queries;

public class GetUserProfileQuery : IRequest<UserProfileDto>
{
    public long UserId { get; set; }
}

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, ct);

        if (user == null)
        {
            throw new NotFoundException("User", query.UserId);
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Currency = user.Currency,
            TimeZone = user.TimeZone,
            Locale = user.Locale,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "User").ToList()
        };
    }
}
