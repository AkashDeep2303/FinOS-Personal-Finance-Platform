namespace FinOS.Common.Exceptions;

/// <summary>
/// Base exception for all domain-level errors in FinOS.
/// Carries a machine-readable error code for API consumers.
/// Maps to HTTP 400 Bad Request unless overridden by a subclass.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException, string errorCode = "DOMAIN_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Machine-readable error code (e.g. "INSUFFICIENT_FUNDS", "INVALID_ACCOUNT").
    /// </summary>
    public string ErrorCode { get; }
}
