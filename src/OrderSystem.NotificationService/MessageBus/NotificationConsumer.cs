using OrderSystem.BusContracts;
using OrderSystem.MessageBus.Abstractions;
using OrderSystem.MessageBus.Rabbit;
using OrderSystem.NotificationService.Services;

namespace OrderSystem.NotificationService.MessageBus;

//public class NotificationConsumer : RabbitBusConsumer<PaymentProcessedEvent>
//{
//    private readonly IServiceProvider _serviceProvider;

//    const string PaymentProcessed = "Processed";
//    const string PaymentFailed = "Failed";
//    protected override string QueueName => "payment.processed";

//    public NotificationConsumer(
//        RabbitBusConnection connection,
//        ILogger<MessageBusConsumer<OrderCreatedEvent>> logger,
//        IServiceProvider serviceProvider) : base(connection, logger)
//    {
//        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//    }

//    protected override async Task HandleMessageAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
//    {
//        using var scope = _serviceProvider.CreateScope();
//        var publisher = scope.ServiceProvider.GetRequiredService<IMessageBusPublisher>();
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationConsumer>>();

//        logger.LogInformation("Received OrderCreatedEvent: {@Event}", message);
//        logger.LogInformation("Processing payment for Order {OrderId}", message.OrderId);
//        var success = new Random().NextDouble() > 0.4;
//        var status = success ? PaymentProcessed : PaymentFailed;

//        var processedEvent = new PaymentProcessedEvent(message.OrderId, status);
//        await publisher.PublishAsync(processedEvent, cancellationToken);

//        logger.LogInformation("Payment processed for Order ID: {OrderId}. Status: {Status}", message.OrderId, status);
//    }

//    protected override Task<bool> OnFailureHandler(PaymentProcessedEvent message, Exception exception, CancellationToken cancellationToken)
//    {
//        ConsumerLogger.LogError(exception, "Failed to process Order {OrderId}", message.OrderId);
//        return Task.FromResult(false);
//    }
//}

public class NotificationConsumer : RabbitBusConsumer<PaymentProcessedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    protected override string QueueName => "payment.processed";

    public NotificationConsumer(
        RabbitBusConnection connection,
        ILogger<MessageBusConsumer<PaymentProcessedEvent>> logger,
        IServiceProvider serviceProvider) : base(connection, logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task HandleMessageAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationConsumer>>();

        logger.LogInformation("Received PaymentProcessedEvent: OrderId={OrderId}, Status={Status}", message.OrderId, message.Status);

        var notificationMessage = $"Payment for Order {message.OrderId} {message.Status} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        await notificationService.SendNotificationAsync(notificationMessage, cancellationToken);

        logger.LogInformation("Sent notification for OrderId={OrderId}", message.OrderId);
    }

    protected override Task<bool> OnFailureHandler(PaymentProcessedEvent message, Exception exception, CancellationToken cancellationToken)
    {
        ConsumerLogger.LogError(exception, "Failed to process PaymentProcessedEvent for OrderId={OrderId}", message.OrderId);
        return Task.FromResult(false);
    }
}