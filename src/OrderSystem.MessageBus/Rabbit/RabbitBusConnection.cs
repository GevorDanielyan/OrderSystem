using RabbitMQ.Client;

namespace OrderSystem.MessageBus.Rabbit;

/// <summary>
/// Represents rabbit bus connection with configuration
/// </summary>
public class RabbitBusConnection : IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Creates new rabbit bus connection
    /// </summary>
    /// <param name="clientName">Client service instance unique name</param>
    /// <param name="hostName">Host name</param>
    /// <param name="user">Username</param>
    /// <param name="password">Password</param>
    /// <param name="virtualHost">Rabbit virtual host</param>
    internal RabbitBusConnection(string clientName, string hostName, string user, string password, string virtualHost = "/")
    {
        _connectionFactory = new ConnectionFactory()
        {
            ClientProvidedName = clientName,
            HostName = hostName,
            UserName = user,
            Password = password,
            VirtualHost = virtualHost,
            AutomaticRecoveryEnabled = true,
        };
    }

    /// <summary>
    /// Creates new rabbit bus connection with ssl
    /// </summary>
    /// <param name="clientName">Client service instance unique name</param>
    /// <param name="hostName">Host name</param>
    /// <param name="user">Username</param>
    /// <param name="password">Password</param>
    /// <param name="sslOptions">Ssl options</param>
    /// <param name="virtualHost">Rabbit virtual host</param>
    internal RabbitBusConnection(string clientName, string hostName, string user, string password, SslOption sslOptions, string virtualHost = "/")
    {
        _connectionFactory = new ConnectionFactory()
        {
            ClientProvidedName = clientName,
            HostName = hostName,
            UserName = user,
            Password = password,
            VirtualHost = virtualHost,
            AutomaticRecoveryEnabled = true,
            Ssl = sslOptions
        };
    }

    /// <summary>
    /// Creates new channel
    /// </summary>
    internal async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new InvalidOperationException("Rabbit bus connection was already disposed");
        }

        if (_connection is null || !_connection.IsOpen)
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        }

        return await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            var disposeTask = _connection?.DisposeAsync();
            _disposed = true;

            return disposeTask ?? ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }
}
