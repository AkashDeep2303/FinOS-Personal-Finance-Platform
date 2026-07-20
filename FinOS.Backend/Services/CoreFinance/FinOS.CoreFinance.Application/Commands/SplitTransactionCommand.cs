using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class SplitTransactionCommand : IRequest<List<TransactionDto>>
{
    public long UserId { get; set; }
    public long TransactionId { get; set; }
    public SplitTransactionRequest Request { get; set; } = new();
}

public class SplitTransactionCommandHandler : IRequestHandler<SplitTransactionCommand, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SplitTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TransactionDto>> Handle(SplitTransactionCommand command, CancellationToken ct)
    {
        var parentTransaction = await _transactionRepository.GetByIdAsync(command.TransactionId, ct);
        if (parentTransaction == null || parentTransaction.UserId != command.UserId)
            throw new NotFoundException("Transaction", command.TransactionId);

        if (parentTransaction.DeletedAt != null)
            throw new DomainException("TRANSACTION_DELETED", "Cannot split a deleted transaction.");

        if (parentTransaction.IsSplit)
            throw new DomainException("ALREADY_SPLIT", "Transaction is already split.");

        var splits = command.Request.Splits;
        var totalSplitAmount = splits.Sum(s => s.Amount);

        if (totalSplitAmount > parentTransaction.Amount)
            throw new DomainException("SPLIT_EXCEEDS", "Total split amount exceeds original transaction amount.");

        // Mark parent as split
        parentTransaction.IsSplit = true;
        parentTransaction.UpdatedAt = DateTime.UtcNow;
        await _transactionRepository.UpdateAsync(parentTransaction);

        var result = new List<TransactionDto>();

        foreach (var split in splits)
        {
            var childTransaction = new Transaction
            {
                UserId = command.UserId,
                AccountId = parentTransaction.AccountId,
                CategoryId = split.CategoryId ?? parentTransaction.CategoryId,
                TransferAccountId = parentTransaction.TransferAccountId,
                Type = parentTransaction.Type,
                Amount = split.Amount,
                Currency = parentTransaction.Currency,
                Description = parentTransaction.Description,
                Notes = split.Notes ?? parentTransaction.Notes,
                TransactionDate = parentTransaction.TransactionDate,
                TransactionTime = parentTransaction.TransactionTime,
                ValueDate = parentTransaction.ValueDate,
                ReferenceNumber = parentTransaction.ReferenceNumber,
                MerchantName = parentTransaction.MerchantName,
                MerchantCategory = parentTransaction.MerchantCategory,
                IsRecurring = false,
                IsSplit = false,
                ParentTransactionId = parentTransaction.Id,
                SplitNote = split.Notes,
                Source = parentTransaction.Source,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(childTransaction, ct);

            result.Add(new TransactionDto
            {
                Id = childTransaction.Id,
                UserId = childTransaction.UserId,
                AccountId = childTransaction.AccountId,
                CategoryId = childTransaction.CategoryId,
                TransferAccountId = childTransaction.TransferAccountId,
                Type = childTransaction.Type.ToString(),
                Amount = childTransaction.Amount,
                Currency = childTransaction.Currency,
                Description = childTransaction.Description,
                Notes = childTransaction.Notes,
                TransactionDate = childTransaction.TransactionDate,
                TransactionTime = childTransaction.TransactionTime,
                ValueDate = childTransaction.ValueDate,
                MerchantName = childTransaction.MerchantName,
                IsRecurring = childTransaction.IsRecurring,
                IsSplit = childTransaction.IsSplit,
                ParentTransactionId = childTransaction.ParentTransactionId,
                SplitNote = childTransaction.SplitNote,
                Source = childTransaction.Source.ToString(),
                IsVerified = childTransaction.IsVerified,
                CreatedAt = childTransaction.CreatedAt
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return result;
    }
}
