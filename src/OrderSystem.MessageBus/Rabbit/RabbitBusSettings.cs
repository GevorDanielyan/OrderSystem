using OrderSystem.MessageBus.Common;
using RabbitMQ.Client;

namespace OrderSystem.MessageBus.Rabbit;
internal class RabbitBusPublishSettings
{
    internal BasicProperties BasicProperties => new()
    {
        ContentType = "application/json",
        Persistent = true,
    };

    internal Dictionary<Type, PublishRoute> PublishRoutesByMessageType = [];

    internal ConnectionRetryPolicy? PublishConnectionRetryPolicy;
}

internal record PublishRoute(string ExchangeName, string RoutingKey);
