using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;

namespace FinOS.CoreFinance.Application.Services;

public interface IBalanceUpdateService
{
    Task ApplyTransactionAsync(Account account, Transaction transaction, CancellationToken ct = default);
    Task ReverseTransactionAsync(Account account, Transaction transaction, CancellationToken ct = default);
}

public class BalanceUpdateService : IBalanceUpdateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FinOS.CoreFinance.Domain.Interfaces.IAccountRepository _accountRepository;

    public BalanceUpdateService(
        IUnitOfWork unitOfWork,
        FinOS.CoreFinance.Domain.Interfaces.IAccountRepository accountRepository)
    {
        _unitOfWork = unitOfWork;
        _accountRepository = accountRepository;
    }

    public async Task ApplyTransactionAsync(Account account, Transaction transaction, CancellationToken ct = default)
    {
        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance += transaction.Amount;
                break;
            case TransactionType.Expense:
                account.Balance -= transaction.Amount;
                break;
            case TransactionType.Transfer:
                account.Balance -= transaction.Amount;
                if (transaction.TransferAccountId.HasValue)
                {
                    var transferAccount = await _accountRepository.GetByIdAsync(transaction.TransferAccountId.Value, ct);
                    if (transferAccount != null)
                    {
                        transferAccount.Balance += transaction.Amount;
                        transferAccount.UpdatedAt = DateTime.UtcNow;
                        await _accountRepository.UpdateAsync(transferAccount);
                    }
                }
                break;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);
    }

    public async Task ReverseTransactionAsync(Account account, Transaction transaction, CancellationToken ct = default)
    {
        switch (transaction.Type)
        {
            case TransactionType.Income:
                account.Balance -= transaction.Amount;
                break;
            case TransactionType.Expense:
                account.Balance += transaction.Amount;
                break;
            case TransactionType.Transfer:
                account.Balance += transaction.Amount;
                if (transaction.TransferAccountId.HasValue)
                {
                    var transferAccount = await _accountRepository.GetByIdAsync(transaction.TransferAccountId.Value, ct);
                    if (transferAccount != null)
                    {
                        transferAccount.Balance -= transaction.Amount;
                        transferAccount.UpdatedAt = DateTime.UtcNow;
                        await _accountRepository.UpdateAsync(transferAccount);
                    }
                }
                break;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(account);
    }
}
