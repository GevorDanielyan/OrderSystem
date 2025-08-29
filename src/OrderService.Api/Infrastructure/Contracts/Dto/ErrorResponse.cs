using System.Net;
using System.ComponentModel.DataAnnotations;
using OrderSystem.Infra.Contracts.Exceptions;

namespace OrderService.Api.Infrastructure.Contracts.Dto;
/// <summary>
/// Represents a standard error response.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets the HTTP status code of the error response.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets the error message associated with the response.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a default error response with an optional message.
    /// </summary>
    /// <param name="message">The optional custom error message. If not provided, a default message is used.</param>
    /// <returns>An <see cref="ErrorResponse"/> with a default status code of 500 (Internal Server Error).</returns>
    public static ErrorResponse Default(string? message = null) => new()
    {
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Message = string.IsNullOrWhiteSpace(message) ? "Unexpected error occurred" : message
    };

    /// <summary>
    /// Creates a default error response from the provided exception.
    /// </summary>
    /// <param name="exception">The exception to extract the error message from.</param>
    /// <returns>An <see cref="ErrorResponse"/> with a default status code of 500 (Internal Server Error) and the exception message.</returns>
    public static ErrorResponse DefaultFromException(Exception exception) => new()
    {
        StatusCode = (int)HttpStatusCode.InternalServerError,
        Message = exception.Message
    };

    /// <summary>
    /// Creates a bad request error response with a custom message.
    /// </summary>
    /// <param name="message">The custom error message describing the bad request.</param>
    /// <returns>An <see cref="ErrorResponse"/> with a status code of 400 (Bad Request) and the provided message.</returns>
    public static ErrorResponse BadRequest(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Message = message
    };

    /// <summary>
    /// Creates a not found error response with a custom message.
    /// </summary>
    /// <param name="message">The custom error message describing the missing resource.</param>
    /// <returns>An <see cref="ErrorResponse"/> with a status code of 404 (Not Found).</returns>
    public static ErrorResponse NotFound(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.NotFound,
        Message = message
    };

    /// <summary>
    /// Creates a conflict error response with a custom message.
    /// </summary>
    /// <returns>An <see cref="ErrorResponse"/> with a status code of 409 (Conflict).</returns>
    public static ErrorResponse Conflict(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.Conflict,
        Message = message
    };

    /// <summary>
    /// Creates a forbidden error response with a custom message.
    /// </summary>
    /// <returns>An <see cref="ErrorResponse"/> with a status code of 403 (resource is forbidden).</returns>
    public static ErrorResponse Forbidden(string message) => new()
    {
        StatusCode = (int)HttpStatusCode.Forbidden,
        Message = message
    };

    /// <summary>
    /// Creates an error response from the provided exception.
    /// </summary>
    /// <param name="exception">The exception to map to an error response.</param>
    /// <returns>The mapped <see cref="ErrorResponse"/>.</returns>
    public static ErrorResponse FromException(Exception exception) => exception switch
    {
        EntityNotFoundException e => NotFound(e.Message),
        OperationRestrictedException e => Forbidden(e.Message),
        ValidationException or
            ArgumentOutOfRangeException or
            ArgumentException => BadRequest(exception.Message),
        _ => DefaultFromException(exception)
    };

    /// <summary>
    /// Converts <see cref="ErrorResponse"/> from other service into concrete exception
    /// </summary>
    public Exception IntoException()
    {
        if (StatusCode < (int)HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException($"Current {nameof(StatusCode)} is successful status code, it can't be mapped into exception");
        }

        return (HttpStatusCode)StatusCode switch
        {
            HttpStatusCode.NotFound => new EntityNotFoundException(Message ?? "Couldn't find requested entity"),
            HttpStatusCode.Forbidden => new OperationRestrictedException(Message ?? "Operation is restricted"),
            HttpStatusCode.BadRequest => new ValidationException(Message ?? "This operation is not correct or input params are invalid"),
            _ => new ApiClientUnhandledException(Message ?? "Unexpected error occurred during the API request")
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Status code: {StatusCode}, Message: {Message ?? string.Empty}";
    }
}

