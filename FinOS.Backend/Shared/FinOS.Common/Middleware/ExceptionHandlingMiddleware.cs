using System.Net;
using System.Text.Json;
using FinOS.Common.Exceptions;
using FinOS.Common.Models;
using Microsoft.AspNetCore.Http;

namespace FinOS.Common.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and converts them into a consistent <see cref="ApiResponse"/> JSON envelope.
///
/// Mapping:
///   NotFoundException       → 404 Not Found
///   ValidationException     → 422 Unprocessable Entity
///   DomainException         → 400 Bad Request
///   UnauthorizedAccessException → 401 Unauthorized
///   Everything else         → 500 Internal Server Error
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                ApiResponse.Fail(notFoundEx.Message, notFoundEx.ErrorCode)),

            ValidationException validationEx => (
                HttpStatusCode.UnprocessableEntity,
                ApiResponse.Fail(validationEx.Message, validationEx.ErrorCode, validationEx.Errors)),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(domainEx.Message, domainEx.ErrorCode)),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse.Fail("You are not authorized to perform this action.", "UNAUTHORIZED")),

            _ => HandleUnknownException(exception)
        };

        _logger.LogError(exception,
            "Unhandled exception: {ExceptionType} — {Message}. HTTP {StatusCode}",
            exception.GetType().Name,
            exception.Message,
            (int)statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }

    private (HttpStatusCode, ApiResponse) HandleUnknownException(Exception exception)
    {
        // In production, do not leak internal details.
        // In development, include the exception message for debugging.
        var message = "An unexpected error occurred. Please try again later.";

        return (HttpStatusCode.InternalServerError, ApiResponse.Fail(message, "INTERNAL_SERVER_ERROR"));
    }
}

/// <summary>
/// Extension method to register <see cref="ExceptionHandlingMiddleware"/> in the pipeline.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the global <see cref="ExceptionHandlingMiddleware"/> to the application pipeline.
    /// Should be called early in the pipeline (before other middleware).
    /// </summary>
    public static IApplicationBuilder UseFinOSExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
