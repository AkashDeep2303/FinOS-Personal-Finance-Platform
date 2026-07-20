using FinOS.CoreFinance.Application.Commands;
using FluentValidation;

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
    }

    private static bool BeValidType(string type)
    {
        return new[] { "Income", "Expense", "Transfer" }.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
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
