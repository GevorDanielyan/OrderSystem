using Npgsql;
using FluentMigrator.Runner;
using OrderSystem.BusContracts;
using Microsoft.AspNetCore.Builder;
using OrderSystem.MessageBus.Rabbit;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Repositories;
using OrderService.Infrastructure.Migrations;
using OrderService.Infrastructure.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Database.Context;

namespace OrderService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentMigrator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
            .AddPostgres()
            .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"))
            .ScanIn(typeof(_1_CreateOrdersTable).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceCollection AddOrderServiceRepositories(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }

    public static IServiceCollection AddDapperContext(this IServiceCollection services, IConfiguration configuration)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton(sp => new DapperContext(configuration));
        return services;
    }

    public static IApplicationBuilder Migrate(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var migrator = scope.ServiceProvider.GetService<IMigrationRunner>();
        migrator?.ListMigrations();
        migrator?.MigrateUp();
        return app;
    }

    /// <summary>
    /// Ensures that target database exists and creates it if it does not
    /// </summary>
    public static void EnsureDatabase(IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("DefaultConnection");
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(postgresConnectionString);
        var targetDb = connectionStringBuilder.Database!;
        connectionStringBuilder.Database = "postgres";
        using var postgresConnection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        postgresConnection.Open();

        if (!DatabaseExists(postgresConnection, targetDb))
        {
            CreateDatabase(postgresConnection, targetDb);
        }
    }

    public static void SetupMessageBus(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var busConnection = configuration.GetSection(nameof(BusConnection)).Get<BusConnection>();

        services
            .UseRabbitBus(serviceName, busConnection!.HostName, busConnection.User, busConnection.Password)
            .AddPublisher<OrderCreatedEvent>("order.exchange", "order.created")
            .AddConsumer<PaymentProcessedConsumer, PaymentProcessedEvent>()
            .UseDefaultPublisherConnectionRetryPolicy()
            .Build();
    }

    private static bool DatabaseExists(NpgsqlConnection defaultPostgresDatabaseConnection, string databaseName)
    {
        using var command = defaultPostgresDatabaseConnection.CreateCommand();
        command.CommandText = $"SELECT 1 FROM pg_database WHERE datname = @dbname";
        _ = command.Parameters.Add(new NpgsqlParameter("@dbname", databaseName));
        var result = command.ExecuteScalar();
        var isExists = result != null;

        return isExists;
    }

    private static void CreateDatabase(NpgsqlConnection defaultPostgresDatabaseConnection, string databaseName)
    {
        using var command = defaultPostgresDatabaseConnection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";

        _ = command.ExecuteNonQuery();
    }

    private record BusConnection(string HostName, string User, string Password);
}

