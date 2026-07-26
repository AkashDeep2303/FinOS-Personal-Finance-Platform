using System.Text;
using FinOS.Common.Helpers;
using Xunit;

namespace FinOS.Common.Tests;

public sealed class CsvTransactionValidatorTests
{
    [Fact]
    public void Validate_ParsesDebitCreditIndianStatement()
    {
        var result = CsvTransactionValidator.Validate(
            Encoding.UTF8.GetBytes("Txn Date,Narration,Withdrawal,Deposit\n26/07/2026,Coffee,\"1,250.50\",\n25/07/2026,Salary,,100000"),
            new Dictionary<string, string?>
            {
                ["transactionDate"] = "Txn Date", ["description"] = "Narration",
                ["debit"] = "Withdrawal", ["credit"] = "Deposit"
            },
            "Income");

        Assert.Equal(2, result.ValidRows);
        Assert.Equal("Expense", result.SampleTransactions[0].Type);
        Assert.Equal(1250.50m, result.SampleTransactions[0].Amount);
        Assert.Equal("Income", result.SampleTransactions[1].Type);
    }

    [Fact]
    public void Validate_UsesExplicitAmountSignConvention()
    {
        var mappings = new Dictionary<string, string?>
        {
            ["transactionDate"] = "Date", ["description"] = "Description", ["amount"] = "Amount"
        };
        var result = CsvTransactionValidator.Validate(
            Encoding.UTF8.GetBytes("Date,Description,Amount\n2026-07-26,Refund,500\n2026-07-25,Purchase,-200"),
            mappings,
            "Income");

        Assert.Equal("Income", result.SampleTransactions[0].Type);
        Assert.Equal("Expense", result.SampleTransactions[1].Type);
    }

    [Fact]
    public void Validate_ReturnsSafeRowErrors()
    {
        var result = CsvTransactionValidator.Validate(
            Encoding.UTF8.GetBytes("Date,Description,Debit,Credit\nbad,,10,20"),
            new Dictionary<string, string?>
            {
                ["transactionDate"] = "Date", ["description"] = "Description",
                ["debit"] = "Debit", ["credit"] = "Credit"
            },
            "Income");

        Assert.Equal(0, result.ValidRows);
        Assert.Equal(1, result.InvalidRows);
        Assert.Equal(2, result.Errors[0].RowNumber);
        Assert.DoesNotContain("10", string.Join(" ", result.Errors[0].Errors));
    }
}
