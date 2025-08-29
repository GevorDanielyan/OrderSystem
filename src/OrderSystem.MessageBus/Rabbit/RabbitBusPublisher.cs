using Polly;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderSystem.MessageBus.Abstractions;
using OrderSystem.MessageBus.Common;

namespace OrderSystem.MessageBus.Rabbit;

/// <summary>
/// Rabbit bus exchange publisher
/// </summary>
internal class RabbitBusPublisher : IMessageBusPublisher
{
    // TODO: Move into configuration maybe
    private const int PrefetchCount = 10;

    private readonly RabbitBusPublishSettings _publishSettings;
    private readonly RabbitBusConnection _connection;
    private readonly ILogger<IMessageBusPublisher> _logger;

    /// <summary>
    /// Creates new <see cref="RabbitBusPublisher"/>
    /// </summary>
    /// <param name="publishSettings">Publish settings with basic properties, exchange mapping and connection retry policy</param>
    /// <param name="connection">Rabbit bus connection</param>
    /// <param name="correlationProvider">Optional correlation provider to provide CorrelationId in headers</param>
    /// <param name="correlationOptions">Optional correlation settings</param>
    /// <param name="logger">Context logger</param>
    /// <exception cref="InvalidOperationException">Throws when correlationProvider and it's settings are not both null or have value</exception>
    public RabbitBusPublisher(
        RabbitBusPublishSettings publishSettings,
        RabbitBusConnection connection,
        ILogger<IMessageBusPublisher> logger)
    {
        _publishSettings = publishSettings;
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Publishes specified message into rabbit bus
    /// </summary>
    /// <typeparam name="TMessage">Type of publishing message. If specified publisher is not registered in DI container, <see cref="InvalidOperationException"/> will be thrown</typeparam>
    /// <param name="message">Message to publish</param>
    /// <param name="cancellationToken">cts</param>
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        var publisherRegistered = _publishSettings.PublishRoutesByMessageType.TryGetValue(typeof(TMessage), out var publishRoute);
        if (!publisherRegistered)
        {
            throw new InvalidOperationException($"Publisher is not registered for message type {typeof(TMessage)}");
        }

        var json = JsonSerializer.Serialize(message, CommonOptions.SerializerOptions);
        var body = Encoding.UTF8.GetBytes(json);

        try
        {
            if (_publishSettings.PublishConnectionRetryPolicy is not null)
            {
                var policy = CommonOptions.ConnectionExceptionsPolicyBuilder
                    .WaitAndRetryAsync(
                        _publishSettings.PublishConnectionRetryPolicy.RetryCount,
                        _publishSettings.PublishConnectionRetryPolicy.SleepDurationProvider,
                        (exception, retryNum) => _logger.LogError(
                            "Connection failed. {RetryNum} attempt to publish the message. Error: {Exception}",
                                retryNum, exception));

                await policy.ExecuteAsync(async () => await CreateChannelAndPublish(publishRoute!, body, cancellationToken));
            }
            else
            {
                await CreateChannelAndPublish(publishRoute!, body, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Couldn't publish message ({Message}). Error: {Exception}", json, ex);
        }
    }

    private async Task CreateChannelAndPublish(PublishRoute publishRoute, byte[] body, CancellationToken cancellationToken)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken);

        await channel.BasicQosAsync(0, PrefetchCount, global: false, cancellationToken);

        var props = _publishSettings.BasicProperties;

        await channel.BasicPublishAsync(
            publishRoute!.ExchangeName,
            publishRoute.RoutingKey,
            mandatory: false,
            props,
            body,
            cancellationToken);
    }
}