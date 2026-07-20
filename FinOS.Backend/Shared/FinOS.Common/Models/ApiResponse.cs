namespace FinOS.Common.Models;

/// <summary>
/// Standardised API response envelope used across all FinOS endpoints.
/// Every response — success or failure — is wrapped in this shape
/// so that clients always know how to parse the payload.
/// </summary>
/// <typeparam name="T">Type of the data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The data payload when <see cref="Success"/> is <c>true</c>.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Human-readable message (error description on failure, confirmation on success).
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Machine-readable error code for programmatic handling (e.g. "VALIDATION_ERROR").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Validation or field-level errors, keyed by property name.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// ISO-8601 timestamp of the response.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // ── Factory methods ──────────────────────────────────────────────

    /// <summary>
    /// Creates a successful response with the given data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure response with an error message and optional code.
    /// </summary>
    public static ApiResponse<T> Fail(
        string message,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic convenience alias for responses that carry no data payload.
/// </summary>
public sealed class ApiResponse : ApiResponse<object?>
{
    /// <summary>
    /// Creates a successful response without a data payload.
    /// </summary>
    public static ApiResponse Ok(string message = "Request completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure response without a data payload.
    /// </summary>
    public new static ApiResponse Fail(
        string message,
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };
    }
}
