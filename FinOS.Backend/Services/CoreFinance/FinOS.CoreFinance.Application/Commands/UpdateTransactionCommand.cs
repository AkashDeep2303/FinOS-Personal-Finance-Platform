using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class UpdateTransactionCommand : IRequest<TransactionDto>
{
    public long UserId { get; set; }
    public long TransactionId { get; set; }
    public UpdateTransactionRequest Request { get; set; } = new();
}

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceUpdateService _balanceUpdateService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionCommandHandler(
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

    public async Task<TransactionDto> Handle(UpdateTransactionCommand command, CancellationToken ct)
    {
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, ct);
        if (transaction == null || transaction.UserId != command.UserId)
            throw new NotFoundException("Transaction", command.TransactionId);

        if (transaction.DeletedAt != null)
            throw new DomainException("TRANSACTION_DELETED", "Cannot update a deleted transaction.");

        var account = await _accountRepository.GetByIdAsync(transaction.AccountId, ct)
            ?? throw new NotFoundException("Account", transaction.AccountId);

        // Reverse the old balance effect
        await _balanceUpdateService.ReverseTransactionAsync(account, transaction, ct);

        // Update transaction fields
        if (!Enum.TryParse<TransactionType>(command.Request.Type, true, out var newType))
            throw new DomainException("INVALID_TYPE", $"Invalid transaction type: {command.Request.Type}");

        transaction.CategoryId = command.Request.CategoryId;
        transaction.TransferAccountId = command.Request.TransferAccountId;
        transaction.Type = newType;
        transaction.Amount = command.Request.Amount;
        transaction.Currency = command.Request.Currency ?? "INR";
        transaction.ExchangeRate = command.Request.ExchangeRate;
        transaction.OriginalAmount = command.Request.OriginalAmount;
        transaction.OriginalCurrency = command.Request.OriginalCurrency;
        transaction.Description = command.Request.Description;
        transaction.Notes = command.Request.Notes;
        transaction.TransactionDate = command.Request.TransactionDate;
        transaction.TransactionTime = command.Request.TransactionTime;
        transaction.ValueDate = command.Request.ValueDate;
        transaction.ReferenceNumber = command.Request.ReferenceNumber;
        transaction.MerchantName = command.Request.MerchantName;
        transaction.MerchantCategory = command.Request.MerchantCategory;
        transaction.IsRecurring = command.Request.IsRecurring;
        transaction.RecurringScheduleId = command.Request.RecurringScheduleId;
        transaction.IsFlagged = command.Request.IsFlagged;
        transaction.AttachmentUrls = command.Request.AttachmentUrls != null
            ? string.Join(";", command.Request.AttachmentUrls)
            : null;
        transaction.LocationLat = command.Request.LocationLat;
        transaction.LocationLng = command.Request.LocationLng;
        transaction.LocationName = command.Request.LocationName;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _transactionRepository.UpdateAsync(transaction);

        // Apply new balance effect
        await _balanceUpdateService.ApplyTransactionAsync(account, transaction, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return new TransactionDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            AccountId = transaction.AccountId,
            AccountName = account.Name,
            CategoryId = transaction.CategoryId,
            TransferAccountId = transaction.TransferAccountId,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            ExchangeRate = transaction.ExchangeRate,
            OriginalAmount = transaction.OriginalAmount,
            OriginalCurrency = transaction.OriginalCurrency,
            Description = transaction.Description,
            Notes = transaction.Notes,
            TransactionDate = transaction.TransactionDate,
            TransactionTime = transaction.TransactionTime,
            ValueDate = transaction.ValueDate,
            ReferenceNumber = transaction.ReferenceNumber,
            MerchantName = transaction.MerchantName,
            MerchantCategory = transaction.MerchantCategory,
            IsRecurring = transaction.IsRecurring,
            RecurringScheduleId = transaction.RecurringScheduleId,
            IsFlagged = transaction.IsFlagged,
            IsSplit = transaction.IsSplit,
            ParentTransactionId = transaction.ParentTransactionId,
            SplitNote = transaction.SplitNote,
            AttachmentUrls = transaction.AttachmentUrls?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
            LocationLat = transaction.LocationLat,
            LocationLng = transaction.LocationLng,
            LocationName = transaction.LocationName,
            Source = transaction.Source.ToString(),
            IsVerified = transaction.IsVerified,
            VerifiedAt = transaction.VerifiedAt,
            CreatedAt = transaction.CreatedAt
        };
    }
}
