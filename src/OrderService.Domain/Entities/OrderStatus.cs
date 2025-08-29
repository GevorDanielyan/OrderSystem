namespace OrderService.Domain.Entities;

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus : int
{
    Pending = 0,
    Processed = 1,
    Failed = 2
}