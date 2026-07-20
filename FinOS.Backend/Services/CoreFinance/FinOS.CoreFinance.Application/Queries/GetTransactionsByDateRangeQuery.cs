using FinOS.Common.Models;
using FinOS.CoreFinance.Application.DTOs;
using FinOS.CoreFinance.Domain.Enums;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public class GetTransactionsByDateRangeQuery : IRequest<PagedResult<TransactionDto>>
{
    public long UserId { get; set; }
    public TransactionFilterDto Filter { get; set; } = new();
}

public class GetTransactionsByDateRangeQueryHandler : IRequestHandler<GetTransactionsByDateRangeQuery, PagedResult<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsByDateRangeQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsByDateRangeQuery query, CancellationToken ct)
    {
        var filter = query.Filter;
        var startDate = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = filter.EndDate ?? DateTime.UtcNow;

        TransactionType? type = null;
        if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<TransactionType>(filter.Type, true, out var t))
            type = t;

        var pagedQuery = new PagedQuery
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            SearchTerm = filter.SearchTerm,
            SortBy = filter.SortBy,
            SortDirection = filter.SortDirection
        };

        var result = await _transactionRepository.GetByDateRangeAsync(
            query.UserId, startDate, endDate,
            pagedQuery, type,
            filter.AccountId, filter.CategoryId,
            filter.MerchantName, ct);

        return new PagedResult<TransactionDto>
        {
            Items = result.Items.Select(t => new TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                AccountId = t.AccountId,
                AccountName = t.Account?.Name ?? string.Empty,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                TransferAccountId = t.TransferAccountId,
                TransferAccountName = t.TransferAccount?.Name,
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
                CreatedAt = t.CreatedAt,
                Tags = t.Tags?.Select(tt => new TagDto
                {
                    Id = tt.TagId,
                    Name = tt.Tag?.Name ?? string.Empty,
                    Color = tt.Tag?.Color
                }).ToList() ?? new()
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.PageNumber,
            PageSize = result.PageSize
        };
    }
}
