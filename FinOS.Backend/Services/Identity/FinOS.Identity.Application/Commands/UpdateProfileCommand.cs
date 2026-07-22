using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public class UpdateProfileCommand : IRequest<DTOs.UserProfileDto>
{
    public long UserId { get; set; }
    public DTOs.UpdateProfileRequest Request { get; set; } = null!;
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, DTOs.UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DTOs.UserProfileDto> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, ct);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        var request = command.Request;

        // Update only provided fields
        if (request.FirstName is not null)
            user.FirstName = request.FirstName.Trim();

        if (request.LastName is not null)
            user.LastName = request.LastName.Trim();

        if (request.PhoneNumber is not null)
            user.PhoneNumber = request.PhoneNumber.Trim();

        if (request.DateOfBirth is not null)
            user.DateOfBirth = request.DateOfBirth.Value.Date;

        if (request.Bio is not null)
            user.Bio = request.Bio.Trim();

        if (request.ProfileImageUrl is not null)
            user.ProfileImageUrl = request.ProfileImageUrl;

        if (request.Currency is not null)
            user.Currency = request.Currency;

        if (request.TimeZone is not null)
            user.TimeZone = request.TimeZone;

        if (request.Locale is not null)
            user.Locale = request.Locale;

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return new DTOs.UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Bio = user.Bio,
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
