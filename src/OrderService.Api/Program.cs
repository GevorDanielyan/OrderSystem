using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Api.Infrastructure.Logging;
using OrderService.Api.Infrastructure.Extensions;
using DbInitializationExtension = OrderService.Infrastructure.ServiceCollectionExtensions;

namespace OrderService.Api;

public class Program
{
    private const string SERVICE_NAME = "order-service-api";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddOrderSystemLogging(builder.Configuration, SERVICE_NAME);
        builder.Services.AddOrderServiceControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        DbInitializationExtension.EnsureDatabase(builder.Configuration);
        builder.Services.AddDapperContext(builder.Configuration);
        builder.Services.AddFluentMigrator(builder.Configuration);
        builder.Services.AddOrderServiceRepositories();
        builder.Services.SetupMessageBus(builder.Configuration, SERVICE_NAME);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrderApplicationDummyClass).Assembly));

        var app = builder.Build();
        app.UseOrderSystemLogging();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Migrate();

        app.UseHttpsRedirection();
        app.UseExceptionHandler("/error");
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
