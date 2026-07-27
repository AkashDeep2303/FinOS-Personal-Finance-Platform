using System.Text;

namespace FinOS.Common.Helpers;

public sealed record CsvPreviewResult(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    int DataRowCount,
    IReadOnlyDictionary<string, string> SuggestedMappings);

public sealed record CsvMappingValidationResult(
    bool IsValid,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed record CsvDocument(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public static class CsvPreviewParser
{
    public const int MaxColumns = 50;
    public const int MaxDataRows = 5000;
    public const int MaxCellLength = 500;
    public const int SampleRowCount = 5;

    public static CsvPreviewResult Parse(byte[] content)
    {
        var document = ParseDocument(content);
        return new CsvPreviewResult(
            document.Headers,
            document.Rows.Take(SampleRowCount).ToList(),
            document.Rows.Count,
            CsvColumnMapping.Suggest(document.Headers));
    }

    public static CsvDocument ParseDocument(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0) throw new ArgumentException("CSV file is empty.", nameof(content));

        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            throw new ArgumentException("CSV file must use valid UTF-8 encoding.", nameof(content));
        }

        if (text.IndexOf('\0') >= 0) throw new ArgumentException("CSV file contains unsupported binary content.", nameof(content));

        var delimiter = DetectDelimiter(text);
        var rows = ParseRows(text, delimiter);
        if (rows.Count < 2) throw new ArgumentException("CSV file must contain a header and at least one data row.", nameof(content));

        foreach (var row in rows)
            for (var index = 0; index < row.Count; index++)
                row[index] = CleanBankExportValue(row[index]);

        var headerIndex = FindTransactionHeader(rows);
        if (headerIndex < 0) headerIndex = 0;

        var trailingEmptyHeaderCells = rows[headerIndex]
            .AsEnumerable()
            .Reverse()
            .TakeWhile(string.IsNullOrWhiteSpace)
            .Count();
        if (trailingEmptyHeaderCells > 0)
            foreach (var row in rows.Skip(headerIndex))
                RemoveTrailingEmptyCells(row, trailingEmptyHeaderCells);

        var headers = rows[headerIndex].Select(value => value.Trim()).ToArray();
        if (headers.Length == 0 || headers.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("CSV headers cannot be empty.", nameof(content));
        if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length)
            throw new ArgumentException("CSV headers must be unique.", nameof(content));

        var rowsAfterHeader = rows.Skip(headerIndex + 1);
        if (headerIndex > 0)
            rowsAfterHeader = rowsAfterHeader.TakeWhile(row => row.Any(value => !string.IsNullOrWhiteSpace(value)));

        var dataRows = rowsAfterHeader
            .Select((row, index) => new { Row = row, CsvRowNumber = headerIndex + index + 2 })
            .Where(item => item.Row.Any(value => !string.IsNullOrWhiteSpace(value)))
            .ToList();
        if (dataRows.Count == 0) throw new ArgumentException("CSV file has no data rows.", nameof(content));
        var malformed = dataRows.FirstOrDefault(item => item.Row.Count != headers.Length);
        if (malformed is not null)
            throw new ArgumentException(
                $"CSV row {malformed.CsvRowNumber} contains {malformed.Row.Count} columns; the header contains {headers.Length}. " +
                "Descriptions containing commas must be enclosed in double quotes.",
                nameof(content));

        return new CsvDocument(
            headers,
            dataRows.Select(item => (IReadOnlyList<string>)item.Row).ToList());
    }

    private static void RemoveTrailingEmptyCells(List<string> row, int maximumCells)
    {
        while (maximumCells-- > 0 && row.Count > 0 && string.IsNullOrWhiteSpace(row[^1]))
            row.RemoveAt(row.Count - 1);
    }

    private static char DetectDelimiter(string text)
    {
        var sampleLines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Take(50);
        var commaScore = sampleLines.Select(line => line.Count(character => character == ',')).DefaultIfEmpty().Max();
        var pipeScore = sampleLines.Select(line => line.Count(character => character == '|')).DefaultIfEmpty().Max();
        return pipeScore > commaScore ? '|' : ',';
    }

    private static int FindTransactionHeader(IReadOnlyList<List<string>> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var suggestions = CsvColumnMapping.Suggest(rows[index]);
            if (suggestions.ContainsKey("transactionDate") &&
                suggestions.ContainsKey("description") &&
                (suggestions.ContainsKey("amount") ||
                 (suggestions.ContainsKey("debit") && suggestions.ContainsKey("credit"))))
                return index;
        }
        return -1;
    }

    private static string CleanBankExportValue(string value)
    {
        var cleaned = value.Trim();
        if (cleaned.Length >= 2 && cleaned[0] == '~' && cleaned[^1] == '~')
            cleaned = cleaned[1..^1].Trim();
        return cleaned;
    }

    private static List<List<string>> ParseRows(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    Append(cell, '"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    Append(cell, character);
                }
                continue;
            }

            if (character == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (character == delimiter)
            {
                AddCell(row, cell);
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                AddCell(row, cell);
                AddRow(rows, row);
                row = [];
            }
            else
            {
                Append(cell, character);
            }
        }

        if (quoted) throw new ArgumentException("CSV file contains an unterminated quoted value.", nameof(text));
        if (cell.Length > 0 || row.Count > 0)
        {
            AddCell(row, cell);
            AddRow(rows, row);
        }
        return rows;
    }

    private static void Append(StringBuilder cell, char value)
    {
        if (cell.Length >= MaxCellLength) throw new ArgumentException($"CSV values cannot exceed {MaxCellLength} characters.");
        cell.Append(value);
    }

    private static void AddCell(List<string> row, StringBuilder cell)
    {
        if (row.Count >= MaxColumns) throw new ArgumentException($"CSV files cannot exceed {MaxColumns} columns.");
        row.Add(cell.ToString());
        cell.Clear();
    }

    private static void AddRow(List<List<string>> rows, List<string> row)
    {
        if (rows.Count > MaxDataRows) throw new ArgumentException($"CSV files cannot exceed {MaxDataRows} data rows.");
        rows.Add(row);
    }
}

public static class CsvColumnMapping
{
    private static readonly IReadOnlyDictionary<string, string[]> Aliases =
        new Dictionary<string, string[]>
        {
            ["transactionDate"] = ["date", "transactiondate", "txndate", "valuedate"],
            ["description"] = ["description", "narration", "particulars", "remarks", "transactionremarks"],
            ["amount"] = ["amount", "amt", "transactionamount", "txnamount"],
            ["debit"] = ["debit", "withdrawal", "withdrawals", "debitamount"],
            ["credit"] = ["credit", "deposit", "deposits", "creditamount"],
            ["referenceNumber"] = ["reference", "referencenumber", "refno", "transactionid", "utr"],
            ["type"] = ["type", "transactiontype", "drcr", "debitcredit"]
        };

    public static IReadOnlyDictionary<string, string> Suggest(IReadOnlyList<string> headers)
    {
        var normalized = headers
            .GroupBy(Normalize, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var suggestions = new Dictionary<string, string>();
        foreach (var (field, aliases) in Aliases)
        {
            var alias = aliases.FirstOrDefault(normalized.ContainsKey);
            if (alias is not null) suggestions[field] = normalized[alias];
        }
        return suggestions;
    }

    public static CsvMappingValidationResult Validate(
        IReadOnlyList<string> headers,
        IReadOnlyDictionary<string, string?> mappings)
    {
        var errors = new Dictionary<string, List<string>>();
        var allowedFields = Aliases.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var headerSet = headers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = mappings
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);

        foreach (var (field, header) in selected)
        {
            if (!allowedFields.Contains(field)) Add(errors, field, "Unsupported mapping field.");
            if (!headerSet.Contains(header)) Add(errors, field, "Mapped column is not present in the CSV headers.");
        }

        Require(selected, errors, "transactionDate");
        Require(selected, errors, "description");

        var hasAmount = selected.ContainsKey("amount");
        var hasDebit = selected.ContainsKey("debit");
        var hasCredit = selected.ContainsKey("credit");
        if (!hasAmount && !(hasDebit && hasCredit))
            Add(errors, "amount", "Map either Amount, or both Debit and Credit columns.");
        if (hasAmount && (hasDebit || hasCredit))
            Add(errors, "amount", "Use either Amount or Debit/Credit mapping, not both.");

        foreach (var duplicate in selected.GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            foreach (var pair in duplicate) Add(errors, pair.Key, "A CSV column can map to only one financial field.");

        return new CsvMappingValidationResult(
            errors.Count == 0,
            errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static void Require(
        IReadOnlyDictionary<string, string> mappings,
        IDictionary<string, List<string>> errors,
        string field)
    {
        if (!mappings.ContainsKey(field)) Add(errors, field, "This mapping is required.");
    }

    private static void Add(IDictionary<string, List<string>> errors, string field, string error)
    {
        if (!errors.TryGetValue(field, out var fieldErrors)) errors[field] = fieldErrors = [];
        fieldErrors.Add(error);
    }
}
