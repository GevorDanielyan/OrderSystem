using OrderSystem.BusContracts;
using OrderSystem.MessageBus.Rabbit;
using OrderSystem.MessageBus.Abstractions;

namespace OrderSystem.PaymentService.MessageBus;

public class PaymentConsumer : RabbitBusConsumer<OrderCreatedEvent>
{
    private readonly IServiceProvider _serviceProvider;

    const string PaymentProcessed = "Processed";
    const string PaymentFailed = "Failed";
    protected override string QueueName => "order.created";

    public PaymentConsumer(
        RabbitBusConnection connection,
        ILogger<MessageBusConsumer<OrderCreatedEvent>> logger,
        IServiceProvider serviceProvider) : base(connection, logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task HandleMessageAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentConsumer>>();

        logger.LogInformation("Received OrderCreatedEvent: {@Event}", message);
        logger.LogInformation("Processing payment for Order {OrderId}", message.OrderId);
        var success = new Random().NextDouble() > 0.4;
        var status = success ? PaymentProcessed : PaymentFailed;

        var processedEvent = new PaymentProcessedEvent(message.OrderId, status);
        await publisher.PublishAsync(processedEvent, cancellationToken);

        logger.LogInformation("Payment processed for Order ID: {OrderId}. Status: {Status}", message.OrderId, status);
    }

    protected override Task<bool> OnFailureHandler(OrderCreatedEvent message, Exception exception, CancellationToken cancellationToken)
    {
        ConsumerLogger.LogError(exception, "Failed to process Order {OrderId}", message.OrderId);
        return Task.FromResult(false);
    }
}