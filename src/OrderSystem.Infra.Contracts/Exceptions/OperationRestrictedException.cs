namespace OrderSystem.Infra.Contracts.Exceptions;

/// <summary>
/// Indicates that an operation is restricted or forbidden.
/// </summary>
public class OperationRestrictedException : Exception
{
    /// <inheritdoc />
    public OperationRestrictedException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public OperationRestrictedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
