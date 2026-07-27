using FinOS.Common.Exceptions;
using FinOS.CoreFinance.Domain.Entities;
using FinOS.CoreFinance.Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace FinOS.CoreFinance.Application.Commands;

public sealed record AddFinancialDocumentCommand(long UserId, FinancialDocument Document) : IRequest<FinancialDocument>;
public sealed record DeleteFinancialDocumentCommand(long UserId, long Id) : IRequest;
public sealed record UploadFinancialDocumentFileCommand(
    long UserId, long DocumentId, string FileName, string ContentType, byte[] Content) : IRequest<FinancialDocument>;
public sealed record DownloadFinancialDocumentFileQuery(long UserId, long DocumentId) : IRequest<FinancialDocumentFile>;
public sealed record FinancialDocumentFile(string FileName, string ContentType, byte[] Content);

public sealed class AddFinancialDocumentHandler(IFinancialDocumentRepository repository)
    : IRequestHandler<AddFinancialDocumentCommand, FinancialDocument>
{
    public Task<FinancialDocument> Handle(AddFinancialDocumentCommand request, CancellationToken cancellationToken)
    {
        request.Document.UserId = request.UserId;
        request.Document.Status = "Recorded";
        return repository.AddAsync(request.Document, cancellationToken);
    }
}

public sealed class UploadFinancialDocumentFileHandler(
    IFinancialDocumentRepository repository,
    IFinancialDocumentStorage storage)
    : IRequestHandler<UploadFinancialDocumentFileCommand, FinancialDocument>
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedFiles =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".csv"] = ["text/csv", "application/csv", "text/plain"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
        };

    public async Task<FinancialDocument> Handle(UploadFinancialDocumentFileCommand request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetStorageMetadataAsync(request.DocumentId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("FinancialDocument", request.DocumentId);
        if (!string.IsNullOrWhiteSpace(metadata.StorageKey))
            throw new ValidationException("file", "This document already has file content.");

        var safeName = Path.GetFileName(request.FileName);
        var extension = Path.GetExtension(safeName);
        if (string.IsNullOrWhiteSpace(safeName) || !AllowedFiles.TryGetValue(extension, out var contentTypes) ||
            !contentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException("file", "Allowed document formats are PDF, JPG, PNG, CSV, and XLSX.");
        if (request.Content.Length == 0 || request.Content.Length > 10 * 1024 * 1024)
            throw new ValidationException("file", "Document files must be between 1 byte and 10 MB.");
        if (!HasValidSignature(extension, request.Content))
            throw new ValidationException("file", "The file content does not match its extension.");

        var sha256 = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        var key = await storage.SaveAsync(request.UserId, request.Content, cancellationToken);
        try
        {
            if (!await repository.AttachFileAsync(
                    request.DocumentId, request.UserId, key, safeName, request.ContentType,
                    request.Content.LongLength, sha256, cancellationToken))
                throw new ValidationException("file", "File content could not be attached to this document.");
        }
        catch
        {
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        return (await repository.GetAsync(request.UserId, cancellationToken))
            .Single(x => x.Id == request.DocumentId);
    }

    private static bool HasValidSignature(string extension, byte[] content) =>
        extension.ToLowerInvariant() switch
        {
            ".pdf" => content.AsSpan().StartsWith("%PDF-"u8),
            ".jpg" or ".jpeg" => content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff,
            ".png" => content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".xlsx" => content.Length >= 4 && content[0] == 0x50 && content[1] == 0x4b && content[2] == 0x03 && content[3] == 0x04,
            ".csv" => !content.AsSpan().Contains((byte)0),
            _ => false
        };
}

public sealed class DownloadFinancialDocumentFileHandler(
    IFinancialDocumentRepository repository,
    IFinancialDocumentStorage storage)
    : IRequestHandler<DownloadFinancialDocumentFileQuery, FinancialDocumentFile>
{
    public async Task<FinancialDocumentFile> Handle(DownloadFinancialDocumentFileQuery request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetStorageMetadataAsync(request.DocumentId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("FinancialDocument", request.DocumentId);
        if (string.IsNullOrWhiteSpace(metadata.StorageKey))
            throw new NotFoundException("This document has no stored file.");
        var bytes = await storage.ReadAsync(metadata.StorageKey, cancellationToken);
        return new FinancialDocumentFile(
            metadata.OriginalFileName ?? "financial-document",
            metadata.ContentType ?? "application/octet-stream",
            bytes);
    }
}

public sealed class DeleteFinancialDocumentHandler(
    IFinancialDocumentRepository repository,
    IFinancialDocumentStorage storage)
    : IRequestHandler<DeleteFinancialDocumentCommand>
{
    public async Task Handle(DeleteFinancialDocumentCommand request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetStorageMetadataAsync(request.Id, request.UserId, cancellationToken);
        if (!await repository.DeleteAsync(request.Id, request.UserId, cancellationToken))
        {
            throw new NotFoundException("FinancialDocument", request.Id);
        }
        if (!string.IsNullOrWhiteSpace(metadata?.StorageKey))
            await storage.DeleteAsync(metadata.StorageKey, cancellationToken);
    }
}
