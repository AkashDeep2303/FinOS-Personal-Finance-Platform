using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class UpdateAccountCommand : IRequest<AccountDto>
{
    public long UserId { get; set; }
    public long AccountId { get; set; }
    public UpdateAccountRequest Request { get; set; } = new();
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountDto> Handle(UpdateAccountCommand command, CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(command.AccountId, ct);
        if (account == null || account.UserId != command.UserId)
            throw new NotFoundException("Account", command.AccountId);

        if (account.DeletedAt != null)
            throw new DomainException("ACCOUNT_DELETED", "Cannot update a deleted account.");

        account.Name = command.Request.Name;
        account.InstitutionName = command.Request.InstitutionName;
        account.AccountNumber = command.Request.AccountNumber;
        account.Balance = command.Request.Balance;
        account.CreditLimit = command.Request.CreditLimit;
        account.Currency = command.Request.Currency ?? "INR";
        account.Color = command.Request.Color;
        account.Icon = command.Request.Icon;
        account.IsIncludedInNetWorth = command.Request.IsIncludedInNetWorth;
        account.Notes = command.Request.Notes;
        account.IsActive = command.Request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _accountRepository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AccountDto
        {
            Id = account.Id,
            UserId = account.UserId,
            AccountTypeId = account.AccountTypeId,
            Name = account.Name,
            InstitutionName = account.InstitutionName,
            AccountNumber = account.AccountNumber,
            Balance = account.Balance,
            CreditLimit = account.CreditLimit,
            Currency = account.Currency,
            Color = account.Color,
            Icon = account.Icon,
            IsIncludedInNetWorth = account.IsIncludedInNetWorth,
            IsSynced = account.IsSynced,
            SyncProvider = account.SyncProvider,
            LastSyncedAt = account.LastSyncedAt,
            Notes = account.Notes,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
