using System.Globalization;
using System.Text.Json.Serialization;

namespace FinOS.Common.Helpers;

public sealed record CsvTransactionCandidate(
    int RowNumber,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    string Type,
    string? ReferenceNumber);

public sealed record CsvTransactionRowError(int RowNumber, IReadOnlyList<string> Errors);

public sealed record CsvTransactionValidationResult(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<CsvTransactionCandidate> SampleTransactions,
    IReadOnlyList<CsvTransactionRowError> Errors)
{
    [JsonIgnore]
    public IReadOnlyList<CsvTransactionCandidate> ValidTransactions { get; init; } = [];
}

public static class CsvTransactionValidator
{
    private static readonly string[] DateFormats =
        ["dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd MMM yyyy", "dd-MMM-yyyy"];

    public static CsvTransactionValidationResult Validate(
        byte[] content,
        IReadOnlyDictionary<string, string?> mappings,
        string positiveAmountType)
    {
        if (positiveAmountType is not ("Income" or "Expense"))
            throw new ArgumentException("Positive amount type must be Income or Expense.", nameof(positiveAmountType));

        var document = CsvPreviewParser.ParseDocument(content);
        var mappingValidation = CsvColumnMapping.Validate(document.Headers, mappings);
        if (!mappingValidation.IsValid) throw new ArgumentException("CSV column mapping is invalid.", nameof(mappings));

        var indexes = mappings
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                pair => pair.Key,
                pair => IndexOf(document.Headers, pair.Value!),
                StringComparer.OrdinalIgnoreCase);
        var candidates = new List<CsvTransactionCandidate>();
        var errors = new List<CsvTransactionRowError>();

        for (var index = 0; index < document.Rows.Count; index++)
        {
            var row = document.Rows[index];
            var rowErrors = new List<string>();
            var date = ParseDate(Value(row, indexes, "transactionDate"), rowErrors);
            var description = Value(row, indexes, "description").Trim();
            if (string.IsNullOrWhiteSpace(description)) rowErrors.Add("Description is required.");

            var (amount, type) = ParseAmountAndType(row, indexes, positiveAmountType, rowErrors);
            var reference = indexes.ContainsKey("referenceNumber")
                ? Value(row, indexes, "referenceNumber").Trim()
                : null;

            if (rowErrors.Count > 0)
            {
                errors.Add(new CsvTransactionRowError(index + 2, rowErrors));
                continue;
            }

            candidates.Add(new CsvTransactionCandidate(
                index + 2,
                date!.Value,
                description,
                decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                type,
                string.IsNullOrWhiteSpace(reference) ? null : reference));
        }

        return new CsvTransactionValidationResult(
            document.Rows.Count,
            candidates.Count,
            errors.Count,
            candidates.Take(5).ToList(),
            errors.Take(100).ToList())
        {
            ValidTransactions = candidates
        };
    }

    private static (decimal Amount, string Type) ParseAmountAndType(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> indexes,
        string positiveAmountType,
        ICollection<string> errors)
    {
        if (indexes.ContainsKey("amount"))
        {
            if (!TryMoney(Value(row, indexes, "amount"), out var signed) || signed == 0)
            {
                errors.Add("Amount must be a non-zero number.");
                return default;
            }
            var explicitType = indexes.ContainsKey("type") ? ParseType(Value(row, indexes, "type")) : null;
            if (indexes.ContainsKey("type") && explicitType is null)
            {
                errors.Add("Transaction type must identify debit/expense or credit/income.");
                return default;
            }
            var type = explicitType ?? (signed > 0
                ? positiveAmountType
                : positiveAmountType == "Income" ? "Expense" : "Income");
            return (Math.Abs(signed), type);
        }

        var debitValid = TryOptionalMoney(Value(row, indexes, "debit"), out var debit);
        var creditValid = TryOptionalMoney(Value(row, indexes, "credit"), out var credit);
        if (!debitValid || !creditValid || debit < 0 || credit < 0 || (debit > 0) == (credit > 0))
        {
            errors.Add("Exactly one of debit or credit must contain a positive amount.");
            return default;
        }
        return debit > 0 ? (debit, "Expense") : (credit, "Income");
    }

    private static DateOnly? ParseDate(string value, ICollection<string> errors)
    {
        if (DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        errors.Add("Date must use DD/MM/YYYY, DD-MM-YYYY, YYYY-MM-DD, or DD MMM YYYY.");
        return null;
    }

    private static bool TryMoney(string value, out decimal amount)
    {
        var cleaned = value.Trim().Replace("₹", "").Replace(",", "");
        var parentheses = cleaned.StartsWith('(') && cleaned.EndsWith(')');
        if (parentheses) cleaned = cleaned[1..^1];
        var valid = decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
        if (valid && parentheses) amount = -amount;
        return valid;
    }

    private static bool TryOptionalMoney(string value, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(value)) { amount = 0; return true; }
        return TryMoney(value, out amount);
    }

    private static string? ParseType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "cr" or "credit" or "c" or "deposit" or "income" => "Income",
        "dr" or "debit" or "d" or "withdrawal" or "expense" => "Expense",
        _ => null
    };

    private static int IndexOf(IReadOnlyList<string> headers, string header)
    {
        for (var index = 0; index < headers.Count; index++)
            if (string.Equals(headers[index], header, StringComparison.OrdinalIgnoreCase)) return index;
        return -1;
    }

    private static string Value(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> indexes,
        string field) => row[indexes[field]];
}
