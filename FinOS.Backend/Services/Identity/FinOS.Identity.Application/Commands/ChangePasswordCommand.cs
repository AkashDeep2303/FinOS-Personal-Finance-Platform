using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.Identity.Application.Interfaces;
using FinOS.Identity.Domain.Interfaces;
using MediatR;

namespace FinOS.Identity.Application.Commands;

public class ChangePasswordCommand : IRequest<Unit>
{
    public long UserId { get; set; }
    public DTOs.ChangePasswordRequest Request { get; set; } = null!;
    public string? IpAddress { get; set; }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, ct);

        if (user == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(command.Request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainException("INVALID_PASSWORD", "Current password is incorrect.");
        }

        // Ensure new password is different from current
        if (_passwordHasher.VerifyPassword(command.Request.NewPassword, user.PasswordHash))
        {
            throw new DomainException("SAME_PASSWORD", "New password must be different from the current password.");
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(command.Request.NewPassword);
        user.PasswordSalt = _passwordHasher.GenerateSalt();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
