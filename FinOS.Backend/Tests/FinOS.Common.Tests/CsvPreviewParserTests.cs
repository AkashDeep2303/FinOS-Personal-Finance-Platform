using System.Text;
using FinOS.Common.Helpers;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class CsvPreviewParserTests
{
    [Fact]
    public void Parse_ReturnsHeadersSamplesAndCount()
    {
        var result = CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(
            "Date,Description,Amount\n2026-01-01,\"Coffee, shop\",250\n2026-01-02,Salary,100000"));

        Assert.Equal(["Date", "Description", "Amount"], result.Headers);
        Assert.Equal(2, result.DataRowCount);
        Assert.Equal("Coffee, shop", result.SampleRows[0][1]);
        Assert.Equal("Date", result.SuggestedMappings["transactionDate"]);
        Assert.Equal("Description", result.SuggestedMappings["description"]);
        Assert.Equal("Amount", result.SuggestedMappings["amount"]);
    }

    [Fact]
    public void Parse_SupportsEscapedQuotesAndQuotedNewlines()
    {
        var result = CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(
            "Description,Amount\n\"Annual \"\"fee\"\"\ncharged\",500"));

        Assert.Equal(1, result.DataRowCount);
        Assert.Equal("Annual \"fee\"\ncharged", result.SampleRows[0][0]);
    }

    [Fact]
    public void Parse_IgnoresTrailingEmptyColumnsFromBankExports()
    {
        var result = CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(
            "Date,Description,Amount,,\n2026-01-01,Coffee,250,,\n2026-01-02,Salary,100000,"));

        Assert.Equal(["Date", "Description", "Amount"], result.Headers);
        Assert.Equal(2, result.DataRowCount);
    }

    [Fact]
    public void Parse_ReportsTheMalformedRowAndColumnCounts()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(
                "Date,Description,Amount\n2026-01-01,Coffee, shop,250")));

        Assert.Contains("CSV row 2 contains 4 columns; the header contains 3", exception.Message);
        Assert.Contains("enclosed in double quotes", exception.Message);
    }

    [Fact]
    public void Parse_DetectsTransactionTableInPipeDelimitedBankExport()
    {
        var result = CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(
            """
            Account Number|XXXX
            Statement Date|27/07/2026

            Domestic / International Transactions
            ~Transaction type~|~Customer Name~|~DATE~|~Description~|~AMT~|~Debit /Credit~|~Rewards~|
            ~D~|~Cardholder~|~25/07/2026~|~Coffee shop~|~250.00~|~D~|~0~|
            ~D~|~Cardholder~|~26/07/2026~|~Payment~|~1000.00~|~C~|~0~|

            Rewards Summary
            """));

        Assert.Equal(7, result.Headers.Count);
        Assert.Equal(2, result.DataRowCount);
        Assert.Equal("DATE", result.Headers[2]);
        Assert.Equal("Description", result.Headers[3]);
        Assert.Equal("AMT", result.SuggestedMappings["amount"]);
    }

    [Theory]
    [InlineData("Date,Date\n2026-01-01,2026-01-01")]
    [InlineData("Date,Amount\n2026-01-01")]
    [InlineData("Date,Amount")]
    [InlineData("Date,Amount\n\"unterminated,20")]
    public void Parse_RejectsMalformedCsv(string csv) =>
        Assert.Throws<ArgumentException>(() => CsvPreviewParser.Parse(Encoding.UTF8.GetBytes(csv)));

    [Fact]
    public void Parse_RejectsInvalidUtf8() =>
        Assert.Throws<ArgumentException>(() => CsvPreviewParser.Parse([0xC3, 0x28]));

    [Fact]
    public void Mapping_SuggestsIndianBankStatementAliases()
    {
        var result = CsvColumnMapping.Suggest(["Txn Date", "Narration", "Withdrawal", "Deposit", "Ref No"]);

        Assert.Equal("Txn Date", result["transactionDate"]);
        Assert.Equal("Narration", result["description"]);
        Assert.Equal("Withdrawal", result["debit"]);
        Assert.Equal("Deposit", result["credit"]);
        Assert.Equal("Ref No", result["referenceNumber"]);
    }

    [Fact]
    public void Mapping_ValidatesAmountShapeAndDuplicateColumns()
    {
        var valid = CsvColumnMapping.Validate(
            ["Date", "Narration", "Debit", "Credit"],
            new Dictionary<string, string?>
            {
                ["transactionDate"] = "Date",
                ["description"] = "Narration",
                ["debit"] = "Debit",
                ["credit"] = "Credit"
            });
        Assert.True(valid.IsValid);

        var invalid = CsvColumnMapping.Validate(
            ["Date", "Narration", "Amount"],
            new Dictionary<string, string?>
            {
                ["transactionDate"] = "Date",
                ["description"] = "Narration",
                ["amount"] = "Amount",
                ["referenceNumber"] = "Amount"
            });
        Assert.False(invalid.IsValid);
        Assert.Contains("referenceNumber", invalid.Errors.Keys);
    }
}
