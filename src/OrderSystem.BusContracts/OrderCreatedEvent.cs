namespace OrderSystem.BusContracts;

public record OrderCreatedEvent(Guid OrderId, string CustomerName, decimal Amount, DateTime CreatedAt);
