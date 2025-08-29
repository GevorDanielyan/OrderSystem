namespace OrderSystem.Infra.Contracts.Exceptions;

/// <summary>
/// Indicates that entity is not found
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <inheritdoc />
    public EntityNotFoundException(object id, Type entityType)
        : base($"Couldn't find {entityType.Name} with id {id}")
    {
    }

    /// <inheritdoc />
    public EntityNotFoundException(object id, Type entityType, Exception innerException)
        : base($"Couldn't find {entityType.Name} with id {id}", innerException)
    {
    }

    /// <inheritdoc />
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
