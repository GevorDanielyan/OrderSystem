namespace OrderSystem.MessageBus.Abstractions;

/// <summary>
/// Message bus publisher
/// </summary>
public interface IMessageBusPublisher
{
    /// <summary>
    /// Publishes specified message into bus
    /// </summary>
    /// <typeparam name="TMessage">Type of publishing message</typeparam>
    /// <param name="message">Message to publish</param>
    /// <param name="cancellationToken">cts</param>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class;
}
