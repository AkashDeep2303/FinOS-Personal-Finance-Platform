using FinOS.CoreFinance.Application.Commands;
using FluentValidation;
using System.Text.Json;

namespace FinOS.CoreFinance.Application.Validators;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(200).WithMessage("Account name must not exceed 200 characters");

        RuleFor(x => x.Request.AccountTypeId)
            .GreaterThan(0).WithMessage("Account type is required");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code");

        RuleFor(x => x.Request.Balance)
            .GreaterThanOrEqualTo(0).WithMessage("Balance cannot be negative");

        RuleFor(x => x.Request.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative");
    }
}

public class SaveTaxProfileCommandValidator : AbstractValidator<FinOS.CoreFinance.Application.Commands.SaveTaxProfileCommand>
{
    public SaveTaxProfileCommandValidator()
    {
        RuleFor(x => x.FinancialYear).Matches(@"^\d{4}-\d{2}$");
        RuleFor(x => x.PreferredRegime).Must(x => x is null or "Old" or "New");
        RuleFor(x => x.InputJson).NotEmpty().MaximumLength(20000)
            .Must(BeValidProfileJson).WithMessage("Tax profile input must be valid JSON with non-negative numeric financial values.");
    }

    private static bool BeValidProfileJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            string[] fields = ["salary", "interest", "dividend", "rentalIncome", "capitalGains",
                "otherIncome", "deductions", "tdsPaid", "otherTaxPaid"];
            return fields.All(field =>
                !document.RootElement.TryGetProperty(field, out var valueElement) ||
                valueElement.TryGetDecimal(out var number) && number >= 0);
        }
        catch (JsonException) { return false; }
    }
}

public sealed class CreateTaxRuleVersionValidator : AbstractValidator<CreateTaxRuleVersionCommand>
{
    public CreateTaxRuleVersionValidator()
    {
        RuleFor(x => x.FinancialYear).Matches(@"^\d{4}-\d{2}$");
        RuleFor(x => x.AssessmentYear).Matches(@"^\d{4}-\d{2}$");
        RuleFor(x => x).Must(HasFollowingAssessmentYear)
            .WithMessage("Assessment year must immediately follow the financial year.");
        RuleFor(x => x.Regime).Must(x => x is "Old" or "New");
        RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
        RuleFor(x => x.ConfigurationJson).Must(BeValidTaxConfiguration)
            .WithMessage("Tax configuration must contain ordered, non-overlapping slabs with decimal lowerLimit, optional upperLimit, and ratePct from 0 to 100.");
    }

    private static bool BeValidTaxConfiguration(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("slabIncomeTypes", out var incomeTypes) ||
                incomeTypes.ValueKind != JsonValueKind.Array || incomeTypes.GetArrayLength() == 0 ||
                incomeTypes.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(x.GetString()))) return false;
            if (!document.RootElement.TryGetProperty("slabs", out var slabs) ||
                slabs.ValueKind != JsonValueKind.Array || slabs.GetArrayLength() == 0) return false;
            if (document.RootElement.TryGetProperty("specialIncomeRates", out var specialRates))
            {
                if (specialRates.ValueKind != JsonValueKind.Object) return false;
                foreach (var rateProperty in specialRates.EnumerateObject())
                    if (!rateProperty.Value.TryGetDecimal(out var specialRate) ||
                        specialRate is < 0 or > 100) return false;
            }
            foreach (var property in new[] { "deductionLimit", "rebateThreshold", "rebateAmount" })
                if (document.RootElement.TryGetProperty(property, out var value) &&
                    (!value.TryGetDecimal(out var number) || number < 0)) return false;
            if (document.RootElement.TryGetProperty("cessRatePct", out var cess) &&
                (!cess.TryGetDecimal(out var cessRate) || cessRate is < 0 or > 100)) return false;
            decimal? previousUpper = null;
            var index = 0;
            foreach (var slab in slabs.EnumerateArray())
            {
                if (!slab.TryGetProperty("lowerLimit", out var lowerElement) ||
                    !lowerElement.TryGetDecimal(out var lower) || lower < 0 ||
                    !slab.TryGetProperty("ratePct", out var rateElement) ||
                    !rateElement.TryGetDecimal(out var rate) || rate is < 0 or > 100) return false;
                decimal? upper = null;
                if (slab.TryGetProperty("upperLimit", out var upperElement) &&
                    upperElement.ValueKind != JsonValueKind.Null)
                {
                    if (!upperElement.TryGetDecimal(out var value) || value <= lower) return false;
                    upper = value;
                }
                if (index == 0 && lower != 0) return false;
                if (previousUpper.HasValue && lower != previousUpper.Value) return false;
                if (!upper.HasValue && index != slabs.GetArrayLength() - 1) return false;
                previousUpper = upper;
                index++;
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool HasFollowingAssessmentYear(CreateTaxRuleVersionCommand command)
    {
        if (command.FinancialYear.Length != 7 || command.AssessmentYear.Length != 7 ||
            !int.TryParse(command.FinancialYear[..4], out var financialStart) ||
            !int.TryParse(command.AssessmentYear[..4], out var assessmentStart)) return false;
        return assessmentStart == financialStart + 1;
    }
}

public class AddInsurancePolicyValidator : AbstractValidator<FinOS.CoreFinance.Application.Commands.AddInsurancePolicyCommand>
{
    public AddInsurancePolicyValidator()
    {
        RuleFor(x => x.Policy.PolicyType).Must(x => new[] { "Life", "Health", "Vehicle", "Property", "Other" }.Contains(x));
        RuleFor(x => x.Policy.Provider).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Policy.PolicyNumber).MaximumLength(100);
        RuleFor(x => x.Policy.CoverageAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Policy.PremiumAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Policy.PremiumFrequency).Must(x => new[] { "Monthly", "Quarterly", "HalfYearly", "Annual" }.Contains(x));
    }
}

public class SaveCreditCardDetailsValidator : AbstractValidator<FinOS.CoreFinance.Application.Commands.SaveCreditCardDetailsCommand>
{
    public SaveCreditCardDetailsValidator()
    {
        RuleFor(x => x.Card.StatementDay).InclusiveBetween((byte)1, (byte)31).When(x => x.Card.StatementDay.HasValue);
        RuleFor(x => x.Card.PaymentDueDay).InclusiveBetween((byte)1, (byte)31).When(x => x.Card.PaymentDueDay.HasValue);
        RuleFor(x => x.Card.MinimumAmountDue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Card.TotalAmountDue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Card.AnnualInterestRate).InclusiveBetween(0, 100);
    }
}

public class AddAssetValidator : AbstractValidator<FinOS.CoreFinance.Application.Commands.AddAssetCommand>
{
    public AddAssetValidator()
    {
        RuleFor(x => x.Asset.AssetType).Must(x => new[] { "Property", "Vehicle", "Gold", "Collectible", "Business", "Other" }.Contains(x));
        RuleFor(x => x.Asset.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Asset.PurchaseValue).GreaterThanOrEqualTo(0).When(x => x.Asset.PurchaseValue.HasValue);
        RuleFor(x => x.Asset.CurrentEstimatedValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Asset.ValuationDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.Date);
    }
}

public class AddFinancialDocumentValidator : AbstractValidator<AddFinancialDocumentCommand>
{
    private static readonly string[] Types =
    [
        "BankStatement", "BrokerStatement", "MutualFundStatement", "SalarySlip",
        "Form16", "LoanStatement", "EPF", "Insurance", "Tax", "Other"
    ];

    public AddFinancialDocumentValidator()
    {
        RuleFor(x => x.Document.DocumentType).Must(Types.Contains);
        RuleFor(x => x.Document.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Document.Issuer).MaximumLength(150);
        RuleFor(x => x.Document.FinancialYear).Matches(@"^\d{4}-\d{2}$").When(x => !string.IsNullOrWhiteSpace(x.Document.FinancialYear));
        RuleFor(x => x.Document.DocumentDate).LessThanOrEqualTo(DateTime.UtcNow.Date).When(x => x.Document.DocumentDate.HasValue);
        RuleFor(x => x.Document.Notes).MaximumLength(500);
    }
}

public class ResolveImportErrorValidator : AbstractValidator<ResolveImportErrorCommand>
{
    public ResolveImportErrorValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.TransactionId).GreaterThan(0).When(x => x.TransactionId.HasValue);
    }
}

public class AddDataSourceValidator : AbstractValidator<AddDataSourceCommand>
{
    private static readonly string[] Types = ["Bank", "Broker", "MutualFund", "Salary", "Tax", "Loan", "EPF", "Other"];

    public AddDataSourceValidator()
    {
        RuleFor(x => x.Source.SourceType).Must(Types.Contains);
        RuleFor(x => x.Source.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Source.InstitutionName).MaximumLength(150);
    }
}

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID is required");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(200).WithMessage("Account name must not exceed 200 characters");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code");

        RuleFor(x => x.Request.Balance)
            .GreaterThanOrEqualTo(0).WithMessage("Balance cannot be negative");

        RuleFor(x => x.Request.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative");
    }
}

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("Account ID is required");
    }
}

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Request.AccountId)
            .GreaterThan(0).WithMessage("Account ID is required");

        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidType).WithMessage("Invalid transaction type. Valid values: Income, Expense, Transfer");

        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code");

        RuleFor(x => x.Request.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required");

        When(x => x.Request.Type == "Transfer", () =>
        {
            RuleFor(x => x.Request.TransferAccountId)
                .NotNull().WithMessage("Transfer account is required for transfer transactions")
                .GreaterThan(0).WithMessage("Transfer account ID must be valid");
        });
    }

    private static bool BeValidType(string type)
    {
        return new[] { "Income", "Expense", "Transfer" }.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID is required");

        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidType).WithMessage("Invalid transaction type");

        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Request.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required");
    }

    private static bool BeValidType(string type)
    {
        return new[] { "Income", "Expense", "Transfer" }.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}

public class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID is required");
    }
}

public class SplitTransactionCommandValidator : AbstractValidator<SplitTransactionCommand>
{
    public SplitTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID is required");

        RuleFor(x => x.Request.Splits)
            .NotEmpty().WithMessage("At least one split is required")
            .Must(splits => splits.Count >= 2).WithMessage("At least 2 splits are required");

        RuleForEach(x => x.Request.Splits).ChildRules(split =>
        {
            split.RuleFor(s => s.Amount).GreaterThan(0).WithMessage("Split amount must be greater than zero");
        });
    }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Category type is required")
            .Must(BeValidType).WithMessage("Invalid category type. Valid values: Income, Expense, Transfer");
        RuleFor(x => x.Request.CashFlowClassification)
            .Must(BeValidCashFlowClassification)
            .WithMessage("Invalid cash-flow classification.");
    }

    private static bool BeValidType(string type)
    {
        return new[] { "Income", "Expense", "Transfer" }.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidCashFlowClassification(string value) =>
        new[] { "Essential", "Lifestyle", "EMI", "Investment", "Other" }
            .Contains(value, StringComparer.OrdinalIgnoreCase);
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category ID is required");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");
        RuleFor(x => x.Request.CashFlowClassification)
            .Must(value => new[] { "Essential", "Lifestyle", "EMI", "Investment", "Other" }
                .Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Invalid cash-flow classification.");
    }
}

public class CreateRecurringScheduleCommandValidator : AbstractValidator<CreateRecurringScheduleCommand>
{
    public CreateRecurringScheduleCommandValidator()
    {
        RuleFor(x => x.Request.AccountId)
            .GreaterThan(0).WithMessage("Account ID is required");

        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Request.Type)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidType).WithMessage("Invalid transaction type");

        RuleFor(x => x.Request.Frequency)
            .NotEmpty().WithMessage("Frequency is required")
            .Must(BeValidFrequency).WithMessage("Invalid frequency. Valid values: Daily, Weekly, BiWeekly, Monthly, Quarterly, SemiAnnually, Annually");

        RuleFor(x => x.Request.IntervalValue)
            .GreaterThan(0).WithMessage("Interval must be at least 1");

        RuleFor(x => x.Request.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        When(x => x.Request.EndDate.HasValue, () =>
        {
            RuleFor(x => x.Request.EndDate)
                .GreaterThan(x => x.Request.StartDate).WithMessage("End date must be after start date");
        });
    }

    private static bool BeValidType(string type)
        => new[] { "Income", "Expense", "Transfer" }.Contains(type, StringComparer.OrdinalIgnoreCase);

    private static bool BeValidFrequency(string freq)
        => new[] { "Daily", "Weekly", "BiWeekly", "Monthly", "Quarterly", "SemiAnnually", "Annually" }
            .Contains(freq, StringComparer.OrdinalIgnoreCase);
}
