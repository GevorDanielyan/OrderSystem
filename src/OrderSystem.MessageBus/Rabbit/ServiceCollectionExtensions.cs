using RabbitMQ.Client;
using Microsoft.Extensions.DependencyInjection;
using OrderSystem.MessageBus.Abstractions;
using OrderSystem.MessageBus.Common;

namespace OrderSystem.MessageBus.Rabbit;

/// <summary>
/// DI extensions
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Creates rabbit bus configuration builder
    /// </summary>
    /// <param name="clientName">Client service instance unique name</param>
    /// <param name="services">DI container</param>
    /// <param name="hostName">Host name</param>
    /// <param name="user">Username</param>
    /// <param name="password">Password</param>
    /// <param name="virtualHost">Rabbit virtual host</param>
    public static IMessageBusConfigurationBuilder UseRabbitBus(
        this IServiceCollection services,
        string clientName,
        string hostName,
        string user,
        string password,
        string virtualHost = "/")
    {
        var rabbitBusConnection = new RabbitBusConnection(clientName, hostName, user, password, virtualHost);

        return new RabbitBusConfigurationBuilder(services, rabbitBusConnection);
    }

    /// <summary>
    /// Creates rabbit bus configuration builder
    /// </summary>
    /// <param name="clientName">Client service instance unique name</param>
    /// <param name="services">DI container</param>
    /// <param name="hostName">Host name</param>
    /// <param name="user">Username</param>
    /// <param name="password">Password</param>
    /// <param name="sslOptions">Ssl options</param>
    /// <param name="virtualHost">Rabbit virtual host</param>
    public static IMessageBusConfigurationBuilder UseRabbitBus(
        this IServiceCollection services,
        string clientName,
        string hostName,
        string user,
        string password,
        SslOption sslOptions,
        string virtualHost = "/")
    {
        var rabbitBusConnection = new RabbitBusConnection(clientName, hostName, user, password, sslOptions, virtualHost);

        return new RabbitBusConfigurationBuilder(services, rabbitBusConnection);
    }
}

/// <summary>
/// Rabbit bus configuration builder.
/// </summary>
public class RabbitBusConfigurationBuilder : IMessageBusConfigurationBuilder
{
    private IServiceCollection _services;
    private RabbitBusConnection _connection;
    private RabbitBusPublishSettings? _rabbitBusPublishSettings;
    private List<Action> _consumerHostedServicesInjection = [];

    internal RabbitBusConfigurationBuilder(IServiceCollection services, RabbitBusConnection connection)
    {
        _services = services;
        _connection = connection;
    }

    /// <inheritdoc/>
    public IMessageBusConfigurationBuilder AddPublisher<TMessage>(string exchange, string routingKey)
    {
        if (_rabbitBusPublishSettings is null)
        {
            _rabbitBusPublishSettings = new RabbitBusPublishSettings();
        }

        _rabbitBusPublishSettings.PublishRoutesByMessageType.Add(typeof(TMessage), new PublishRoute(exchange, routingKey));

        return this;
    }

    /// <inheritdoc/>
    public IMessageBusConfigurationBuilder UsePublisherConnectionRetryPolicy(ConnectionRetryPolicy retryPolicy)
    {
        if (_rabbitBusPublishSettings is null)
        {
            _rabbitBusPublishSettings = new RabbitBusPublishSettings();
        }

        _rabbitBusPublishSettings.PublishConnectionRetryPolicy = retryPolicy;

        return this;
    }

    /// <inheritdoc/>
    public IMessageBusConfigurationBuilder UseDefaultPublisherConnectionRetryPolicy() =>
        UsePublisherConnectionRetryPolicy(CommonOptions.DefaultConnectionRetryPolicy);

    /// <inheritdoc/>
    public IMessageBusConfigurationBuilder AddConsumer<TConsumer, TMessage>()
        where TMessage : class
        where TConsumer : MessageBusConsumer<TMessage>
    {
        _consumerHostedServicesInjection.Add(() => _services.AddHostedService<TConsumer>());

        return this;
    }

    /// <inheritdoc/>
    public void Build()
    {
        _services.AddSingleton(_connection);

        if (_rabbitBusPublishSettings is not null)
        {
            _services.AddSingleton(_rabbitBusPublishSettings);
            _services.AddScoped<IMessageBusPublisher, RabbitBusPublisher>();
        }

        foreach (var consumerInjection in _consumerHostedServicesInjection)
        {
            consumerInjection.Invoke();
        }
    }
}