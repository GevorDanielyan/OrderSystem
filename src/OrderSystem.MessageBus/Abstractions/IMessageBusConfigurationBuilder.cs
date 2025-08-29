using OrderSystem.MessageBus.Common;

namespace OrderSystem.MessageBus.Abstractions;

/// <summary>
/// Message bus configuration builder. this.Register() method should be invoked after configuring
/// </summary>
public interface IMessageBusConfigurationBuilder
{
    /// <summary>
    /// Adds publisher for specified messages
    /// </summary>
    /// <typeparam name="TMessage">Type of messages for publisher</typeparam>
    /// <param name="exchange">Exchange name for messages</param>
    /// <param name="routingKey">Routing key for messages</param>
    public IMessageBusConfigurationBuilder AddPublisher<TMessage>(string exchange, string routingKey);

    /// <summary>
    /// Sets retry policy for publishers if connection is failed
    /// </summary>
    /// <param name="retryPolicy">Retry policy to use</param>    
    public IMessageBusConfigurationBuilder UsePublisherConnectionRetryPolicy(ConnectionRetryPolicy retryPolicy);

    /// <summary>
    /// Sets default retry policy for publishers if connection is failed
    /// </summary>
    public IMessageBusConfigurationBuilder UseDefaultPublisherConnectionRetryPolicy();

    /// <summary>
    /// Adds consumer
    /// </summary>
    /// <typeparam name="TConsumer">Type of adding consumer</typeparam>
    /// <typeparam name="TMessage">Type of messages consuming by adding consumer</typeparam>
    public IMessageBusConfigurationBuilder AddConsumer<TConsumer, TMessage>()
        where TMessage : class
        where TConsumer : MessageBusConsumer<TMessage>;

    /// <summary>
    /// Registers message bus services into DI according to builder configuration
    /// </summary>
    public void Build();
}
