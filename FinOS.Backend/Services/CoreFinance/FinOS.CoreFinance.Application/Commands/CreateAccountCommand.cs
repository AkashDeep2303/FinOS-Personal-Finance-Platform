using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class CreateAccountCommand : IRequest<AccountDto>
{
    public long UserId { get; set; }
    public CreateAccountRequest Request { get; set; } = new();
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand command, CancellationToken ct)
    {
        var account = new Account
        {
            UserId = command.UserId,
            AccountTypeId = command.Request.AccountTypeId,
            Name = command.Request.Name,
            InstitutionName = command.Request.InstitutionName,
            AccountNumber = command.Request.AccountNumber,
            Balance = command.Request.Balance,
            CreditLimit = command.Request.CreditLimit,
            Currency = command.Request.Currency ?? "INR",
            Color = command.Request.Color,
            Icon = command.Request.Icon,
            IsIncludedInNetWorth = command.Request.IsIncludedInNetWorth,
            Notes = command.Request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _accountRepository.AddAsync(account, ct);
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
