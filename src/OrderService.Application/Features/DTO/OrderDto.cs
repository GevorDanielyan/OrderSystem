using OrderService.Domain.Entities;

namespace OrderService.Application.Features.DTO;

public record OrderDto(Guid Id, string CustomerName, decimal Amount, DateTime CreatedAt, OrderStatus Status);
