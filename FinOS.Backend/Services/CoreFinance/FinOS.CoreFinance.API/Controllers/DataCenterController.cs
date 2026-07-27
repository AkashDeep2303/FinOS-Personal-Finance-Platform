using FinOS.Common.Models;
using FinOS.CoreFinance.Application.Commands;
using FinOS.CoreFinance.Application.Queries;
using FinOS.CoreFinance.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinOS.CoreFinance.API.Controllers;

[ApiController]
[Route("api/data-center")]
[Authorize]
public sealed class DataCenterController(IMediator mediator) : ControllerBase
{
    private const long MaxCsvBytes = 2 * 1024 * 1024;
    private const long MaxDocumentBytes = 10 * 1024 * 1024;
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DataCenterOverview>>> GetOverview(
        [FromQuery] int importLimit = 20,
        [FromQuery] int issueLimit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var overview = await mediator.Send(
            new GetDataCenterOverviewQuery(userId, importLimit, issueLimit),
            cancellationToken);

        return Ok(ApiResponse<DataCenterOverview>.Ok(overview));
    }

    [HttpPost("imports/csv/preview")]
    [RequestSizeLimit(MaxCsvBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCsvBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<FinOS.Common.Helpers.CsvPreviewResult>>> PreviewCsv(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0)
            throw new FinOS.Common.Exceptions.ValidationException("file", "A non-empty CSV file is required.");
        if (file.Length > MaxCsvBytes)
            throw new FinOS.Common.Exceptions.ValidationException("file", "CSV files cannot exceed 2 MB.");

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var preview = await mediator.Send(
            new PreviewCsvImportQuery(userId, Path.GetFileName(file.FileName), stream.ToArray()),
            cancellationToken);
        return Ok(ApiResponse<FinOS.Common.Helpers.CsvPreviewResult>.Ok(preview));
    }

    [HttpPost("imports/csv/mapping/validate")]
    public async Task<ActionResult<ApiResponse<FinOS.Common.Helpers.CsvMappingValidationResult>>> ValidateCsvMapping(
        [FromBody] ValidateCsvMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request.Headers.Count is < 1 or > FinOS.Common.Helpers.CsvPreviewParser.MaxColumns)
            throw new FinOS.Common.Exceptions.ValidationException("headers", "Provide between 1 and 50 CSV headers.");
        var result = await mediator.Send(
            new ValidateCsvMappingQuery(userId, request.Headers, request.Mappings),
            cancellationToken);
        return Ok(ApiResponse<FinOS.Common.Helpers.CsvMappingValidationResult>.Ok(result));
    }

    [HttpPost("imports/csv/transactions/validate")]
    [RequestSizeLimit(MaxCsvBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCsvBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<FinOS.Common.Helpers.CsvTransactionValidationResult>>> ValidateCsvTransactions(
        [FromForm] IFormFile file,
        [FromForm] string mappings,
        [FromForm] string positiveAmountType,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0)
            throw new FinOS.Common.Exceptions.ValidationException("file", "A non-empty CSV file is required.");
        if (file.Length > MaxCsvBytes)
            throw new FinOS.Common.Exceptions.ValidationException("file", "CSV files cannot exceed 2 MB.");

        IReadOnlyDictionary<string, string?> parsedMappings;
        try
        {
            parsedMappings = JsonSerializer.Deserialize<Dictionary<string, string?>>(mappings)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new FinOS.Common.Exceptions.ValidationException("mappings", "Column mappings must be valid JSON.");
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var result = await mediator.Send(
            new ValidateCsvTransactionsQuery(
                userId,
                Path.GetFileName(file.FileName),
                stream.ToArray(),
                parsedMappings,
                positiveAmountType),
            cancellationToken);
        return Ok(ApiResponse<FinOS.Common.Helpers.CsvTransactionValidationResult>.Ok(result));
    }

    [HttpPost("imports/csv/duplicates/check")]
    [RequestSizeLimit(MaxCsvBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCsvBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportDuplicateAnalysis>>> CheckCsvDuplicates(
        [FromForm] IFormFile file,
        [FromForm] string mappings,
        [FromForm] string positiveAmountType,
        [FromForm] long accountId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0 || file.Length > MaxCsvBytes)
            throw new FinOS.Common.Exceptions.ValidationException("file", "Provide a non-empty CSV file no larger than 2 MB.");
        Dictionary<string, string?> parsedMappings;
        try
        {
            parsedMappings = JsonSerializer.Deserialize<Dictionary<string, string?>>(mappings)
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new FinOS.Common.Exceptions.ValidationException("mappings", "Column mappings must be valid JSON.");
        }
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var result = await mediator.Send(
            new CheckCsvDuplicatesQuery(
                userId, accountId, Path.GetFileName(file.FileName), stream.ToArray(),
                parsedMappings, positiveAmountType),
            cancellationToken);
        return Ok(ApiResponse<ImportDuplicateAnalysis>.Ok(result));
    }

    [HttpPost("imports/csv/confirm")]
    [RequestSizeLimit(MaxCsvBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxCsvBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<CsvImportResult>>> ConfirmCsvImport(
        [FromForm] IFormFile file,[FromForm] string mappings,[FromForm] string positiveAmountType,
        [FromForm] long accountId,[FromForm] string duplicatePolicy,CancellationToken cancellationToken=default)
    {
        if(!TryGetUserId(out var userId))return Unauthorized();
        if(file is null||file.Length==0||file.Length>MaxCsvBytes)throw new FinOS.Common.Exceptions.ValidationException("file","Provide a non-empty CSV file no larger than 2 MB.");
        Dictionary<string,string?> parsed;try{parsed=JsonSerializer.Deserialize<Dictionary<string,string?>>(mappings)??throw new JsonException();}catch(JsonException){throw new FinOS.Common.Exceptions.ValidationException("mappings","Column mappings must be valid JSON.");}
        await using var stream=new MemoryStream((int)file.Length);await file.CopyToAsync(stream,cancellationToken);
        var result=await mediator.Send(new ImportCsvTransactionsCommand(userId,accountId,Path.GetFileName(file.FileName),stream.ToArray(),parsed,positiveAmountType,duplicatePolicy),cancellationToken);
        return Ok(ApiResponse<CsvImportResult>.Ok(result));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FinancialDocument>>>> GetDocuments(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var documents = await mediator.Send(new GetFinancialDocumentsQuery(userId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinancialDocument>>.Ok(documents));
    }

    [HttpGet("sources")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DataSource>>>> GetSources(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var sources = await mediator.Send(new GetDataSourcesQuery(userId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DataSource>>.Ok(sources));
    }

    [HttpPost("sources")]
    public async Task<ActionResult<ApiResponse<DataSource>>> AddSource(
        [FromBody] DataSource source,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var created = await mediator.Send(new AddDataSourceCommand(userId, source), cancellationToken);
        return Ok(ApiResponse<DataSource>.Ok(created));
    }

    [HttpDelete("sources/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSource(
        long id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await mediator.Send(new DeleteDataSourceCommand(userId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("reconciliation-issues")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ImportReconciliationIssue>>>> GetReconciliationIssues(
        [FromQuery] int limit = 100,
        [FromQuery] bool includeResolved = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var issues = await mediator.Send(
            new GetImportReconciliationIssuesQuery(userId, limit, includeResolved),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ImportReconciliationIssue>>.Ok(issues));
    }

    [HttpPost("reconciliation-issues/{id:long}/resolve")]
    public async Task<ActionResult<ApiResponse<object>>> ResolveReconciliationIssue(
        long id,
        [FromBody] ResolveImportErrorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await mediator.Send(new ResolveImportErrorCommand(userId, id, request.TransactionId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("documents")]
    public async Task<ActionResult<ApiResponse<FinancialDocument>>> AddDocument(
        [FromBody] FinancialDocument document,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var created = await mediator.Send(new AddFinancialDocumentCommand(userId, document), cancellationToken);
        return Ok(ApiResponse<FinancialDocument>.Ok(created));
    }

    [HttpPost("documents/{id:long}/file")]
    [RequestSizeLimit(MaxDocumentBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDocumentBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<FinancialDocument>>> UploadDocumentFile(
        long id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0 || file.Length > MaxDocumentBytes)
            throw new FinOS.Common.Exceptions.ValidationException("file", "Provide a non-empty document no larger than 10 MB.");
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var document = await mediator.Send(new UploadFinancialDocumentFileCommand(
            userId, id, Path.GetFileName(file.FileName), file.ContentType, stream.ToArray()), cancellationToken);
        return Ok(ApiResponse<FinancialDocument>.Ok(document));
    }

    [HttpGet("documents/{id:long}/file")]
    public async Task<IActionResult> DownloadDocumentFile(long id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var file = await mediator.Send(new DownloadFinancialDocumentFileQuery(userId, id), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: false);
    }

    public sealed record ResolveImportErrorRequest(long? TransactionId);
    public sealed record ValidateCsvMappingRequest(
        IReadOnlyList<string> Headers,
        IReadOnlyDictionary<string, string?> Mappings);

    [HttpDelete("documents/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDocument(
        long id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await mediator.Send(new DeleteFinancialDocumentCommand(userId, id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    private bool TryGetUserId(out long userId)
    {
        var value = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        return long.TryParse(value, out userId) && userId > 0;
    }
}
