using MediatR;
using OrderSystem.BusContracts;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using System.Text.Json.Serialization;
using OrderSystem.MessageBus.Abstractions;
using OrderService.Application.Repositories;
using OrderService.Application.Features.DTO;

namespace OrderService.Application.Features.Orders;

public record CreateOrderRequest(
    [property: JsonPropertyName("customerName")] string CustomerName,
    [property: JsonPropertyName("amount")] decimal Amount);

public record CreateOrderCommand(IReadOnlyList<CreateOrderRequest> Orders) : IRequest<CreateEntityResponse>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateEntityResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly IMessageBusPublisher _messageBusPublisher;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, ILogger<CreateOrderCommandHandler> logger, IMessageBusPublisher messageBusPublisher)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBusPublisher = messageBusPublisher ?? throw new ArgumentNullException(nameof(messageBusPublisher));
    }

    public async Task<CreateEntityResponse> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {

        if (command.Orders.Count == 0)
        {
            throw new ArgumentException("Order list cannot be empty.", nameof(command.Orders));
        }

        _logger.LogInformation("Creating {OrderCount} orders", command.Orders.Count);

        var orders = new List<Order>(command.Orders.Count);
        foreach (var req in command.Orders)
        {
            var order = new Order(req.CustomerName, req.Amount);
            orders.Add(order);
            _logger.LogInformation("Prepared order with ID: {OrderId}", order.Id);
        }

        var rowsAffected = await _orderRepository.AddRangeAsync(orders, cancellationToken);
        if (rowsAffected != command.Orders.Count)
        {
            _logger.LogWarning("Bulk order creation affected unexpected rows: {RowsAffected} (expected {Expected})", rowsAffected, command.Orders.Count);
            throw new InvalidOperationException("Failed to create all orders in database.");
        }

        var orderIds = new List<Guid>(orders.Count);
        foreach (var order in orders)
        {
            var orderCreatedEvent = new OrderCreatedEvent(order.Id, order.CustomerName, order.Amount, order.CreatedAt);
            await _messageBusPublisher.PublishAsync(orderCreatedEvent, cancellationToken);
            orderIds.Add(order.Id);
        }

        _logger.LogInformation("Successfully created and published {OrderCount} orders", orders.Count);
        return new CreateEntityResponse(orderIds.AsReadOnly());

    }
}