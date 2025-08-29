using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Application.Features.DTO;
using OrderService.Application.Repositories;
using OrderSystem.Infra.Contracts.Exceptions;

namespace OrderService.Application.Features.Orders;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto?>;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    public GetOrderQueryHandler(IOrderRepository orderRepository, ILogger<GetOrderQueryHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new EntityNotFoundException(request.Id, typeof(Order));

        return new OrderDto(order!.Id, order.CustomerName, order.Amount, order.CreatedAt, order.Status);
    }
}
