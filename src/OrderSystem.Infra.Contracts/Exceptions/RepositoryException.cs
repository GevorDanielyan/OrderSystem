namespace OrderSystem.Infra.Contracts.Exceptions;

/// <summary>
/// Custom exception for repo errors
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}
