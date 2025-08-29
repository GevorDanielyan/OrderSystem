namespace OrderSystem.Infra.Contracts.Exceptions;

/// <summary>
/// Indicates an exception when there is an unhandled error when one service invokes other's API
/// </summary>
public class ApiClientUnhandledException : Exception
{
    /// <inheritdoc/>    
    public ApiClientUnhandledException(string message)
        : base(message)
    {
    }

    /// <inheritdoc/>    
    public ApiClientUnhandledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
