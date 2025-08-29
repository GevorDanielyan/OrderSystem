using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Repositories;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderSystem.MessageBus.Rabbit;
using OrderSystem.BusContracts;

namespace OrderService.Infrastructure.MessageBus;

internal class PaymentProcessedConsumer : RabbitBusConsumer<PaymentProcessedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(
        RabbitBusConnection connection,
        IServiceProvider serviceProvider,
        ILogger<PaymentProcessedConsumer> logger)
        : base(connection, logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override string QueueName => "payment.processed";

    protected override async Task HandleMessageAsync(PaymentProcessedEvent message, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Received PaymentProcessedEvent for Order ID: {OrderId}, Status: {Status}",
            message.OrderId, message.Status);

        var status = message.Status switch
        {
            "Processed" => OrderStatus.Processed,
            "Failed" => OrderStatus.Failed,
            _ => throw new ArgumentException($"Invalid payment status: {message.Status}", nameof(message.Status))
        };

        using var scope = _serviceProvider.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var rowsAffected = await orderRepository.UpdateStatusAsync(message.OrderId, status, stoppingToken);

        if (rowsAffected != 1)
        {
            _logger.LogWarning("Failed to update status for Order ID: {OrderId}. Rows affected: {RowsAffected}",
                message.OrderId, rowsAffected);

            throw new InvalidOperationException($"Failed to update order status for Order ID: {message.OrderId}");
        }

        _logger.LogInformation("Updated order status for Order ID: {OrderId} to {Status}", message.OrderId, status);
    }

    protected override Task<bool> OnFailureHandler(PaymentProcessedEvent message, Exception exception, CancellationToken stoppingToken)
    {
        ConsumerLogger.LogError(exception, "Failed to process PaymentProcessedEvent for Order ID: {OrderId}", message.OrderId);
        return Task.FromResult(false);
    }
}
