namespace FinOS.Common.Exceptions;

/// <summary>
/// Thrown when one or more validation rules are violated.
/// Maps to HTTP 422 Unprocessable Entity (or 400 Bad Request depending on middleware config).
/// Carries a dictionary of field-level errors for detailed client feedback.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public ValidationException(string fieldName, string errorMessage)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>
        {
            { fieldName, new[] { errorMessage } }
        };
    }

    /// <summary>
    /// Field-level validation errors keyed by property/field name.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
}
