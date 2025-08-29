using Microsoft.AspNetCore.Builder;
using OrderSystem.Infra.Logging.Logging;
using OrderSystem.PaymentService.Extensions;
using OrderSystem.PaymentService.MessageBus;

namespace OrderSystem.PaymentService
{
    public class Program
    {
        private const string SERVICE_NAME = "payment-service-worker";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddOrderSystemLogging(builder.Configuration, SERVICE_NAME);
            builder.Services.AddHealthChecks();
            builder.Services.SetupMessageBus(builder.Configuration, SERVICE_NAME);
            builder.Services.AddHostedService<PaymentConsumer>();

            var app = builder.Build();
            app.UseOrderSystemLogging();
            app.MapHealthChecks("/health");
            app.Run();
        }
    }
}