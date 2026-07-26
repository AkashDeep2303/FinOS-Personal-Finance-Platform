using FinOS.Common.Exceptions;
using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record PreviewCsvImportQuery(long UserId, string FileName, byte[] Content) : IRequest<CsvPreviewResult>;

public sealed class PreviewCsvImportHandler : IRequestHandler<PreviewCsvImportQuery, CsvPreviewResult>
{
    public Task<CsvPreviewResult> Handle(PreviewCsvImportQuery request, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(request.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("file", "Only .csv files are supported.");

        try
        {
            return Task.FromResult(CsvPreviewParser.Parse(request.Content));
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException("file", exception.Message);
        }
    }
}
