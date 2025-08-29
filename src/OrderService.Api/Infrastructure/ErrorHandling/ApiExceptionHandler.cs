using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using OrderService.Api.Infrastructure.Contracts.Dto;

namespace OrderService.Api.Infrastructure.ErrorHandling;

/// <summary>
/// Common API exception handler
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions SerializingOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ApiExceptionHandler> _logger;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, message: exception.Message);

        var errorResponse = exception is not null
            ? ErrorResponse.FromException(exception)
            : ErrorResponse.Default();

        await WriteErrorResponse(httpContext, errorResponse);

        return true;
    }

    /// <summary>
    /// Writes the error response to the HTTP context response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="errorResponse">The error response to write.</param>
    private static Task WriteErrorResponse(HttpContext context, ErrorResponse errorResponse)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = errorResponse.StatusCode;

        var serializedError = JsonSerializer.Serialize(errorResponse, SerializingOptions);

        return context.Response.WriteAsync(serializedError);
    }
}
