using OrderService.Domain.Entities;

namespace OrderService.Application.Repositories;

/// <summary>
/// Order repository
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds a range of orders
    /// </summary>
    /// <param name="orders">collection of orders</param>
    Task<int> AddRangeAsync(IReadOnlyList<Order> orders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by id
    /// </summary>
    /// <param name="id">order id</param>
    /// <returns>Returns Order object</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an order
    /// </summary>
    /// <param name="id">order id</param>
    /// <param name="status">order status</param>
    Task<int> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}