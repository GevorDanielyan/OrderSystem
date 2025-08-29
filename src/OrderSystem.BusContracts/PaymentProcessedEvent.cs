namespace OrderSystem.BusContracts;

/// <summary>
/// Represents a payment processing outcome.
/// Status values: "Processed" (successful payment), "Failed" (failed payment).
/// </summary>
public record PaymentProcessedEvent(Guid OrderId, string Status);