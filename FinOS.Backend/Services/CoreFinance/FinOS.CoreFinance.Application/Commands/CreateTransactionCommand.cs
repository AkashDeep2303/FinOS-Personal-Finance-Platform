using FinOS.Common.Exceptions;
using FinOS.Common.Interfaces;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Application.Services;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Commands;

public class CreateTransactionCommand : IRequest<TransactionDto>
{
    public long UserId { get; set; }
    public CreateTransactionRequest Request { get; set; } = new();
}

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBalanceUpdateService _balanceUpdateService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionCommandHandler(
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

    public async Task<TransactionDto> Handle(CreateTransactionCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var account = await _accountRepository.GetByIdAsync(request.AccountId, ct);
        if (account == null || account.UserId != command.UserId)
            throw new NotFoundException("Account", request.AccountId);

        if (!Enum.TryParse<TransactionType>(request.Type, true, out var transactionType))
            throw new DomainException("INVALID_TYPE", $"Invalid transaction type: {request.Type}");

        var transaction = new Transaction
        {
            UserId = command.UserId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            TransferAccountId = request.TransferAccountId,
            Type = transactionType,
            Amount = request.Amount,
            Currency = request.Currency ?? "INR",
            ExchangeRate = request.ExchangeRate,
            OriginalAmount = request.OriginalAmount,
            OriginalCurrency = request.OriginalCurrency,
            Description = request.Description,
            Notes = request.Notes,
            TransactionDate = request.TransactionDate,
            TransactionTime = request.TransactionTime,
            ValueDate = request.ValueDate,
            ReferenceNumber = request.ReferenceNumber,
            MerchantName = request.MerchantName,
            MerchantCategory = request.MerchantCategory,
            IsRecurring = request.IsRecurring,
            RecurringScheduleId = request.RecurringScheduleId,
            IsFlagged = request.IsFlagged,
            AttachmentUrls = request.AttachmentUrls != null
                ? string.Join(";", request.AttachmentUrls)
                : null,
            LocationLat = request.LocationLat,
            LocationLng = request.LocationLng,
            LocationName = request.LocationName,
            Source = Enum.TryParse<TransactionSource>(request.Source, true, out var source)
                ? source : TransactionSource.Manual,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction, ct);

        // Update account balance
        await _balanceUpdateService.ApplyTransactionAsync(account, transaction, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(transaction, account.Name);
    }

    private static TransactionDto MapToDto(Transaction t, string accountName)
    {
        return new TransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            AccountId = t.AccountId,
            AccountName = accountName,
            CategoryId = t.CategoryId,
            TransferAccountId = t.TransferAccountId,
            Type = t.Type.ToString(),
            Amount = t.Amount,
            Currency = t.Currency,
            ExchangeRate = t.ExchangeRate,
            OriginalAmount = t.OriginalAmount,
            OriginalCurrency = t.OriginalCurrency,
            Description = t.Description,
            Notes = t.Notes,
            TransactionDate = t.TransactionDate,
            TransactionTime = t.TransactionTime,
            ValueDate = t.ValueDate,
            ReferenceNumber = t.ReferenceNumber,
            MerchantName = t.MerchantName,
            MerchantCategory = t.MerchantCategory,
            IsRecurring = t.IsRecurring,
            RecurringScheduleId = t.RecurringScheduleId,
            IsFlagged = t.IsFlagged,
            IsSplit = t.IsSplit,
            ParentTransactionId = t.ParentTransactionId,
            SplitNote = t.SplitNote,
            AttachmentUrls = t.AttachmentUrls?.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
            LocationLat = t.LocationLat,
            LocationLng = t.LocationLng,
            LocationName = t.LocationName,
            Source = t.Source.ToString(),
            IsVerified = t.IsVerified,
            VerifiedAt = t.VerifiedAt,
            CreatedAt = t.CreatedAt
        };
    }
}
