using OrderSystem.BusContracts;
using OrderSystem.MessageBus.Rabbit;
using OrderSystem.NotificationService.Services;
using OrderSystem.NotificationService.MessageBus;

namespace OrderSystem.NotificationService.Extensions;

public static class ServiceCollectionExtensions
{
    public static void SetupMessageBus(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var busConnection = configuration.GetSection(nameof(BusConnection)).Get<BusConnection>();
        services
            .UseRabbitBus(serviceName, busConnection!.HostName, busConnection.User, busConnection.Password)
            .AddConsumer<NotificationConsumer, PaymentProcessedEvent>()
            .UseDefaultPublisherConnectionRetryPolicy()
            .Build();
    }

    public static IServiceCollection AddOrderSystemSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<INotificationService, NottificationService>();
        return services;
    }

    private record BusConnection(string HostName, string User, string Password);
}
