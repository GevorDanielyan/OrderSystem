using OrderSystem.BusContracts;
using OrderSystem.MessageBus.Rabbit;
using OrderSystem.PaymentService.MessageBus;

namespace OrderSystem.PaymentService.Extensions;

public static class ServiceCollectionExtensions
{
    public static void SetupMessageBus(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var busConnection = configuration.GetSection(nameof(BusConnection)).Get<BusConnection>();

        services
            .UseRabbitBus(serviceName, busConnection!.HostName, busConnection.User, busConnection.Password)
            .AddConsumer<PaymentConsumer, OrderCreatedEvent>()
            .AddPublisher<PaymentProcessedEvent>("payment.exchange", "payment.processed")
            .UseDefaultPublisherConnectionRetryPolicy()
            .Build();
    }

    private record BusConnection(string HostName, string User, string Password);
}
