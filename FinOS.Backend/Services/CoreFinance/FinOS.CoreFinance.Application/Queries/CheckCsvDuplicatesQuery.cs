using FinOS.Common.Exceptions;
using FinOS.Common.Helpers;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using System.Globalization;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record CheckCsvDuplicatesQuery(
    long UserId,
    long AccountId,
    string FileName,
    byte[] Content,
    IReadOnlyDictionary<string, string?> Mappings,
    string PositiveAmountType) : IRequest<ImportDuplicateAnalysis>;

public sealed class CheckCsvDuplicatesHandler(IDataCenterRepository repository)
    : IRequestHandler<CheckCsvDuplicatesQuery, ImportDuplicateAnalysis>
{
    public async Task<ImportDuplicateAnalysis> Handle(
        CheckCsvDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(request.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("file", "Only .csv files are supported.");
        CsvTransactionValidationResult validation;
        try
        {
            validation = CsvTransactionValidator.Validate(request.Content, request.Mappings, request.PositiveAmountType);
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException("file", exception.Message);
        }
        if (validation.InvalidRows > 0)
            throw new ValidationException("file", "Resolve invalid CSV rows before checking duplicates.");

        var analysis = await repository.CheckImportDuplicatesAsync(
            request.UserId,
            request.AccountId,
            validation.ValidTransactions,
            cancellationToken);
        if (!analysis.AccountExists) throw new NotFoundException("Account", request.AccountId);
        var matches = analysis.Matches.ToList();
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in validation.ValidTransactions)
        {
            var key = candidate.ReferenceNumber is not null
                ? $"R|{candidate.ReferenceNumber}"
                : $"V|{candidate.TransactionDate:yyyy-MM-dd}|{candidate.Amount.ToString(CultureInfo.InvariantCulture)}|{candidate.Type}|{candidate.Description}";
            if (seen.TryGetValue(key, out var firstRow) && matches.All(match => match.RowNumber != candidate.RowNumber))
            {
                matches.Add(new ImportDuplicateMatch
                {
                    RowNumber = candidate.RowNumber,
                    MatchingRowNumber = firstRow,
                    MatchReason = "WithinFile"
                });
            }
            else
            {
                seen.TryAdd(key, candidate.RowNumber);
            }
        }
        return new ImportDuplicateAnalysis
        {
            AccountExists = true,
            CandidateRows = analysis.CandidateRows,
            DuplicateRows = matches.Select(match => match.RowNumber).Distinct().Count(),
            Matches = matches.OrderBy(match => match.RowNumber).Take(100).ToList()
        };
    }
}
