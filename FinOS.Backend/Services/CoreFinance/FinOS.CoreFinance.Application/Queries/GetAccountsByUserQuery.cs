using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class GetAccountsByUserQuery : IRequest<List<AccountDto>>
{
    public long UserId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class GetAccountsByUserQueryHandler : IRequestHandler<GetAccountsByUserQuery, List<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountsByUserQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<List<AccountDto>> Handle(GetAccountsByUserQuery query, CancellationToken ct)
    {
        var accounts = query.ActiveOnly
            ? await _accountRepository.GetActiveByUserIdAsync(query.UserId, ct)
            : await _accountRepository.GetByUserIdAsync(query.UserId, ct);

        return accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            UserId = a.UserId,
            AccountTypeId = a.AccountTypeId,
            AccountTypeName = a.AccountType?.Name ?? string.Empty,
            Name = a.Name,
            InstitutionName = a.InstitutionName,
            AccountNumber = a.AccountNumber,
            Balance = a.Balance,
            CreditLimit = a.CreditLimit,
            Currency = a.Currency,
            Color = a.Color,
            Icon = a.Icon,
            IsIncludedInNetWorth = a.IsIncludedInNetWorth,
            IsSynced = a.IsSynced,
            SyncProvider = a.SyncProvider,
            LastSyncedAt = a.LastSyncedAt,
            Notes = a.Notes,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
    }
}
