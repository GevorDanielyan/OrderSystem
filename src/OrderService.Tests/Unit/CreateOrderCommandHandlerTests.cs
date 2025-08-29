using Moq;
using OrderSystem.BusContracts;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderSystem.MessageBus.Abstractions;
using OrderService.Application.Repositories;
using OrderService.Application.Features.Orders;
using OrderService.Application.Features.DTO;

namespace OrderService.Tests.Unit;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockRepo;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger;
    private readonly Mock<IMessageBusPublisher> _mockPublisher;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockRepo = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
        _mockPublisher = new Mock<IMessageBusPublisher>();
        _handler = new CreateOrderCommandHandler(_mockRepo.Object, _mockLogger.Object, _mockPublisher.Object);
    }

    [Fact]
    public async Task Handle_SingleOrder_Success_ReturnsSingleId()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John Smith", 100m)
        }.AsReadOnly());

        _mockRepo.Setup(r => r.AddRangeAsync(It.Is<IReadOnlyList<Order>>(o =>
            o.Count == 1 &&
            o[0].CustomerName == "John Smith" &&
            o[0].Amount == 100m),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPublisher.Setup(p => p.PublishAsync(It.Is<OrderCreatedEvent>(e =>
            e.CustomerName == "John Smith" &&
            e.Amount == 100m),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.CreatedIds);
        Assert.IsAssignableFrom<CreateEntityResponse>(result);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Once());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_MultipleOrders_Success_ReturnsAllIds()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John", 100m),
            new("Jane", 200m)
        }.AsReadOnly());

        _mockRepo.Setup(r => r.AddRangeAsync(It.Is<IReadOnlyList<Order>>(o =>
            o.Count == 2 &&
            o[0].CustomerName == "John" && o[0].Amount == 100m &&
            o[1].CustomerName == "Jane" && o[1].Amount == 200m),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.CreatedIds.Count);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Once());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_EmptyList_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderCommand(Array.Empty<CreateOrderRequest>().AsReadOnly());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(request, CancellationToken.None));
        Assert.Equal("Order list cannot be empty. (Parameter 'Orders')", exception.Message);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Never());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_InvalidCustomerName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("", 100m)
        }.AsReadOnly());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(request, CancellationToken.None));
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Never());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John", -50m)
        }.AsReadOnly());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(request, CancellationToken.None));
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Never());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_BulkOrders_FailedInsert_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John", 100m),
            new("Jane", 200m)
        }.AsReadOnly());

        _mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(request, CancellationToken.None));
        Assert.Equal("Failed to create all orders in database.", exception.Message);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Once());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_PublishFailureAfterInsert_ThrowsException()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John", 100m)
        }.AsReadOnly());

        _mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Publish failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(request, CancellationToken.None));
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Once());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_CancellationRequested_StopsProcessing()
    {
        // Arrange
        var request = new CreateOrderCommand(new List<CreateOrderRequest>
        {
            new("John", 100m)
        }.AsReadOnly());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(request, cts.Token));
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<Order>>(), It.IsAny<CancellationToken>()), Times.Once());
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
