using FinOS.Common.Exceptions;
using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record ValidateCsvTransactionsQuery(
    long UserId,
    string FileName,
    byte[] Content,
    IReadOnlyDictionary<string, string?> Mappings,
    string PositiveAmountType) : IRequest<CsvTransactionValidationResult>;

public sealed class ValidateCsvTransactionsHandler
    : IRequestHandler<ValidateCsvTransactionsQuery, CsvTransactionValidationResult>
{
    public Task<CsvTransactionValidationResult> Handle(
        ValidateCsvTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(request.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("file", "Only .csv files are supported.");
        try
        {
            return Task.FromResult(CsvTransactionValidator.Validate(
                request.Content,
                request.Mappings,
                request.PositiveAmountType));
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException("file", exception.Message);
        }
    }
}
