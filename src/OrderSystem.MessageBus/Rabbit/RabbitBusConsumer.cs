using Polly;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Microsoft.Extensions.Logging;
using OrderSystem.MessageBus.Common;
using OrderSystem.MessageBus.Abstractions;

namespace OrderSystem.MessageBus.Rabbit;

/// <summary>
/// Represents base class for rabbit bus consumers
/// </summary>
public abstract class RabbitBusConsumer<TMessage> : MessageBusConsumer<TMessage>
    where TMessage : class
{
    private readonly RabbitBusConnection _connection;

    private IChannel? _channel;

    /// <summary>
    /// <see cref="RabbitBusConsumer{TMessage}"/> ctor
    /// </summary>
    /// <param name="connection">Rabbit bus connection</param>
    /// <param name="correlationProvider">CorrelationId provider. May be null if correlation id is not required</param>
    /// <param name="correlationOptions">CorrelationId options. May be null of correlation id is not required</param>
    /// <param name="logger">Context logger</param>
    protected RabbitBusConsumer(
        RabbitBusConnection connection,
        ILogger<MessageBusConsumer<TMessage>> logger)
        : base(logger)
    {
        _connection = connection;
    }

    /// <summary>
    /// Queue name for consumer
    /// </summary>
    protected abstract string QueueName { get; }

    /// <inheritdoc/>
    protected override async Task<bool> EstablishConnectionAsync(CancellationToken cancellationToken)
    {
        var reconnected = false;

        if (_channel is null || !_channel.IsOpen)
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken);
            reconnected = true;
        }

        return reconnected;
    }

    /// <inheritdoc/>
    protected override async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Connection must be established before starting the message consumption");
        }

        try
        {
            var _ = await _channel.QueueDeclarePassiveAsync(QueueName, stoppingToken);
        }
        catch (OperationInterruptedException ex)
        {
            throw new InvalidOperationException($"Queue with name {QueueName} probably not exists, details: {ex.Message}");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, eventArgs) =>
        {

            var body = eventArgs.Body.ToArray();
            var rawMessage = Encoding.UTF8.GetString(body);

            try
            {
                var success = await HandleMessage(rawMessage, ConsumerLogger, stoppingToken);

                if (success)
                {
                    await AckMessage(eventArgs.DeliveryTag, ConsumerLogger, stoppingToken);
                    ConsumerLogger.LogInformation("Message acknowledged successfully.");
                }
                else
                {
                    await NackMessage(eventArgs.DeliveryTag, ConsumerLogger, stoppingToken);
                    ConsumerLogger.LogWarning("Message negatively acknowledged for reprocessing.");
                }
            }
            catch (Exception ex)
            {
                ConsumerLogger.LogError(ex, "Unhandled exception during the message processing");
                await NackMessage(eventArgs.DeliveryTag, ConsumerLogger, stoppingToken);
                ConsumerLogger.LogWarning("Message negatively acknowledged due to an unhandled exception.");
            }

        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            stoppingToken);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _channel?.Dispose();

        base.Dispose();
    }

    private async Task<bool> HandleMessage(
        string rawMessage,
        ILogger<MessageBusConsumer<TMessage>> logger,
        CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<TMessage>(rawMessage, CommonOptions.SerializerOptions);

            ConsumerLogger.LogInformation("Got message: {@Message}", message);

            await HandleMessageAsync(message!, stoppingToken);
            return true;
        }
        catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
        {
            // Bad jsons cannot be processed, so, one need to log them and ack to remove from queue
            logger.LogError(ex, "Couldn't deserialize the message");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't handle the message");

            var message = JsonSerializer.Deserialize<TMessage>(rawMessage, CommonOptions.SerializerOptions);

            try
            {
                return await OnFailureHandler(message!, ex, stoppingToken);
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "Couldn't successfully handle the error when processing the message");

                return false;
            }
        }
    }

    private async Task AckMessage(ulong deliveryTag, ILogger<MessageBusConsumer<TMessage>> logger, CancellationToken stoppingToken)
    {
        var policy = CommonOptions.ConnectionExceptionsPolicyBuilder
                .WaitAndRetryForeverAsync( // TODO: Probably infinity acknowledge attempts are not safety
                    CommonOptions.DefaultInfinityRetrySleepDurationProvider,
                    (exception, retryNum, timespan) => logger.LogError(
                        exception,
                        "Connection failed. {RetryNum} attempt to acknowledge the message in {Milliseconds} milliseconds",
                        retryNum, timespan.Milliseconds));

        try
        {
            await policy.ExecuteAsync(
                async () => await _channel!.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't acknowledge message");
        }
    }

    private async Task NackMessage(ulong deliveryTag, ILogger<MessageBusConsumer<TMessage>> logger, CancellationToken stoppingToken)
    {
        var policy = CommonOptions.ConnectionExceptionsPolicyBuilder
            .WaitAndRetryForeverAsync(
                CommonOptions.DefaultInfinityRetrySleepDurationProvider,
                (exception, retryNum, timespan) => logger.LogError(
                    exception,
                    "Connection failed. {RetryNum} attempt to negatively acknowledge the message in {Milliseconds} milliseconds",
                    retryNum, timespan.Milliseconds));

        try
        {
            await policy.ExecuteAsync(
                async () => await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to negatively acknowledge message");
        }
    }
}
