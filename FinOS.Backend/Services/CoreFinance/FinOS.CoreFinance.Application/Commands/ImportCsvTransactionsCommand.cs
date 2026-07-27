using System.Text.Json;using FinOS.Common.Exceptions;using FinOS.Common.Helpers;using FinOS.CoreFinance.Domain.Entities;using FinOS.CoreFinance.Domain.Interfaces;using MediatR;
namespace FinOS.CoreFinance.Application.Commands;
public sealed record ImportCsvTransactionsCommand(long UserId,long AccountId,string FileName,byte[] Content,IReadOnlyDictionary<string,string?> Mappings,string PositiveAmountType,string DuplicatePolicy):IRequest<CsvImportResult>;
public sealed class ImportCsvTransactionsHandler(IDataCenterRepository repository):IRequestHandler<ImportCsvTransactionsCommand,CsvImportResult>
{
 public async Task<CsvImportResult> Handle(ImportCsvTransactionsCommand r,CancellationToken ct)
 {
  if(Path.GetExtension(r.FileName).ToLowerInvariant()!=".csv")throw new ValidationException("file","Only .csv files are supported.");
  if(r.DuplicatePolicy is not("Skip" or "Include"))throw new ValidationException("duplicatePolicy","Choose Skip or Include.");
  CsvTransactionValidationResult v;try{v=CsvTransactionValidator.Validate(r.Content,r.Mappings,r.PositiveAmountType);}catch(ArgumentException e){throw new ValidationException("file",e.Message);}
  if(v.InvalidRows>0)throw new ValidationException("file","Resolve invalid CSV rows before importing.");
  return await repository.ImportTransactionsAsync(r.UserId,r.AccountId,r.FileName,JsonSerializer.Serialize(r.Mappings),v.ValidTransactions,r.DuplicatePolicy,ct);
 }
}
