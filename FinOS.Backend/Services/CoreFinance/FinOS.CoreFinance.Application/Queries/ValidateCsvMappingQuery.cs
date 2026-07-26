using FinOS.Common.Helpers;
using MediatR;

namespace FinOS.CoreFinance.Application.Queries;

public sealed record ValidateCsvMappingQuery(
    long UserId,
    IReadOnlyList<string> Headers,
    IReadOnlyDictionary<string, string?> Mappings) : IRequest<CsvMappingValidationResult>;

public sealed class ValidateCsvMappingHandler : IRequestHandler<ValidateCsvMappingQuery, CsvMappingValidationResult>
{
    public Task<CsvMappingValidationResult> Handle(
        ValidateCsvMappingQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(CsvColumnMapping.Validate(request.Headers, request.Mappings));
}
