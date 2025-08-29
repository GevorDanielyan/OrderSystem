//using OrderSystem.Infra.Logging;
using Microsoft.AspNetCore.Builder;
using OrderSystem.NotificationService.Extensions;
using OrderSystem.NotificationService.MessageBus;

namespace OrderSystem.NotificationService
{
    public class Program
    {
        private const string SERVICE_NAME = "notification-service-worker";
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //builder.Services.AddOrderSystemLogging(builder.Configuration, SERVICE_NAME);
            builder.Services.AddHealthChecks();
            builder.Services.SetupMessageBus(builder.Configuration, SERVICE_NAME);
            builder.Services.AddHostedService<NotificationConsumer>();
            builder.Services.AddOrderSystemSignalRServices();

            var app = builder.Build();
           // app.UseOrderSystemLogging();
            app.MapHealthChecks("/health");
            app.MapHub<NotificationHub>("/ws-notifications");
            app.Run();
        }
    }
}