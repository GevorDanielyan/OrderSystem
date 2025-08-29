namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a customer order
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }

    // For serialization/Dapper
    private Order() { } 

    public Order(string customerName, decimal amount)
    {
        Id = Guid.NewGuid();
        CustomerName = ValidateCustomerName(customerName);
        Amount = ValidateAmount(amount);
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }

    private static string ValidateCustomerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            throw new ArgumentException("Customer name must be between 1 and 255 characters.", nameof(name));
        }
        return name;
    }

    private static decimal ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }
        return amount;
    }
}