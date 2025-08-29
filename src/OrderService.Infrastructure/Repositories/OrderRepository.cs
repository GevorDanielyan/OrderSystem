using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Application.Repositories;
using OrderSystem.Infra.Contracts.Exceptions;
using OrderService.Infrastructure.Database.Context;

namespace OrderService.Infrastructure.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly DapperContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(DapperContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> AddRangeAsync(IReadOnlyList<Order> orders, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO orders (id, customer_name, amount, created_at, status)
            VALUES (@Id, @CustomerName, @Amount, @CreatedAt, @Status)";

        try
        {
            using var connection = _context.CreateConnection();

            int rowsAffected = 0;
            foreach (var order in orders)
            {
                var command = new CommandDefinition(sql, order, cancellationToken: cancellationToken);
                _logger.LogInformation("Inserting order {@Order} into Database (bulk)", order);
                rowsAffected += await connection.ExecuteAsync(command);
            }

            if (rowsAffected != orders.Count)
            {
                _logger.LogWarning("Unexpected rows affected on bulk insert: {RowsAffected} (expected {Expected})", rowsAffected, orders.Count);
            }
            return rowsAffected;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error during AddRangeAsync");
            throw new RepositoryException("Failed to add orders to database.", ex);
        }
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM orders WHERE id = @Id";
        var parameters = new { Id = id };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

        try
        {
            using var connection = _context.CreateConnection();
            
            return await connection.QuerySingleOrDefaultAsync<Order>(command);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error during GetByIdAsync for Order ID: {OrderId}", id);
            throw new RepositoryException("Failed to retrieve order from database.", ex);
        }
    }

    public async Task<int> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE orders SET status = @Status WHERE id = @Id";
        var parameters = new { Id = id, Status = (int)status };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        try
        {
            using var connection = _context.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(command);
            if (rowsAffected != 1)
            {
                _logger.LogWarning("Unexpected rows affected on update: {RowsAffected} for Order ID: {OrderId}", rowsAffected, id);
            }
            return rowsAffected;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error during UpdateStatusAsync for Order ID: {OrderId}", id);
            throw new RepositoryException("Failed to update order status in database.", ex);
        }
    }
}