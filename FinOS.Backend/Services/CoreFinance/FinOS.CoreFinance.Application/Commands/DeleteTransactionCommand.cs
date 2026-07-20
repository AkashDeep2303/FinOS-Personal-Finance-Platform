using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class DeleteTransactionCommand : IRequest<Unit>
{
    public long UserId { get; set; }
    public long TransactionId { get; set; }
}

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceUpdateService _balanceUpdateService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IBalanceUpdateService balanceUpdateService,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _balanceUpdateService = balanceUpdateService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTransactionCommand command, CancellationToken ct)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, ct);
        if (transaction == null || transaction.UserId != command.UserId)
            throw new NotFoundException("Transaction", command.TransactionId);

        if (transaction.DeletedAt != null)
            throw new DomainException("TRANSACTION_DELETED", "Transaction is already deleted.");

        var account = await _accountRepository.GetByIdAsync(transaction.AccountId, ct)
            ?? throw new NotFoundException("Account", transaction.AccountId);

        // Reverse the balance effect
        await _balanceUpdateService.ReverseTransactionAsync(account, transaction, ct);

        // Soft delete
        transaction.DeletedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
