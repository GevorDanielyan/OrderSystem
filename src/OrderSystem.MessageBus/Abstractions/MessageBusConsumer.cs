using Polly;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSystem.MessageBus.Common;

namespace OrderSystem.MessageBus.Abstractions;

/// <summary>
/// Base class for message bus consumers
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class MessageBusConsumer<TMessage> : BackgroundService
    where TMessage : class
{
    // TODO: Move StartingDelay and ConnectionChecksPeriod maybe

    // Delay for consumer's background service to ensure that every other services are started
    private static readonly TimeSpan StartingDelay = TimeSpan.FromSeconds(3);
    // How often connection to the bus is checked
    private static readonly TimeSpan ConnectionChecksPeriod = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Consumer logger
    /// </summary>
    protected readonly ILogger<MessageBusConsumer<TMessage>> ConsumerLogger;

    /// <summary>
    /// <see cref="MessageBusConsumer{TMessage}"/> ctor
    /// </summary>
    /// <param name="logger">Context logger</param>
    protected MessageBusConsumer(ILogger<MessageBusConsumer<TMessage>> logger) : base()
    {
        ConsumerLogger = logger;
    }

    /// <summary>
    /// Establishes connection to message bus
    /// </summary>
    /// <param name="stoppingToken">background service cts</param>
    /// <returns>If new connection is opened</returns>
    protected abstract Task<bool> EstablishConnectionAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Starts consuming messages with established connection
    /// </summary>
    /// <param name="stoppingToken">background service cts</param>
    protected abstract Task StartConsumingAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Handling logic for consumer
    /// </summary>
    protected abstract Task HandleMessageAsync(TMessage message, CancellationToken stoppingToken);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartingDelay, stoppingToken);

        await StartConsumingWithConnectionChecks(stoppingToken);
    }

    /// <summary>
    /// Handles failures that occur while processing a message in the consumer.
    /// </summary>
    /// <param name="message">The message that caused the failure.</param>
    /// <param name="exception">The exception that was thrown during message processing.</param>
    /// <param name="stoppingToken">Cancellation token</param>
    protected abstract Task<bool> OnFailureHandler(TMessage message, Exception exception, CancellationToken stoppingToken);

    private async Task StartConsumingWithConnectionChecks(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) 
        {
            var retryPolicy = CommonOptions.ConnectionExceptionsPolicyBuilder.WaitAndRetryForeverAsync(
                    CommonOptions.DefaultInfinityRetrySleepDurationProvider,
                    (exception, retryNum, timespan) => ConsumerLogger.LogError(
                        exception,
                        "Connection failed. {RetryNum} attempt to reconnect from the consumer in {Milliseconds} milliseconds",
                        retryNum, timespan.Milliseconds));

            await retryPolicy.ExecuteAsync(async () =>
            {
                // base while loop purpose is to check connection, so there won't be reconnection if it's no need in it if !reconnected
                var reconnected = await EstablishConnectionAsync(stoppingToken);
                if (reconnected)
                {
                    ConsumerLogger.LogInformation("Message bus connection is established");

                    await StartConsumingAsync(stoppingToken);
                }
            });

            await Task.Delay(ConnectionChecksPeriod, stoppingToken);
        }
    }
}
