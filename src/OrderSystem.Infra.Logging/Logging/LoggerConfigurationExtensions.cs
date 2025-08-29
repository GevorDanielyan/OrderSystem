using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace OrderSystem.Infra.Logging.Logging;

/// <summary>
/// Provides extension methods for setting up logging in applications.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Adds Serilog to the service collection and sets up lifecycle handling.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration"><see cref="IConfiguration"/></param>
    /// <param name="serviceName">Service name which uses logging</param>
    /// <param name="logsDirectory">Directory where logs are stored</param>
    public static IServiceCollection AddOrderSystemLogging(this IServiceCollection services, IConfiguration configuration, string serviceName, string logsDirectory = "/app/logs")
    {
        // Configure Serilog globally
        ConfigureLogging(configuration, serviceName, logsDirectory);

        services.AddSingleton(Log.Logger);

        // Register Serilog in the logging factory
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
        return services;
    }

    /// <summary>
    /// Ensures Serilog is correctly integrated with application lifecycle events.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseOrdersSystemLogging(this IApplicationBuilder app)
    {
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        return app;
    }

    /// <summary>
    /// Ensures Serilog is correctly integrated with application lifecycle events for worker services.
    /// </summary>
    public static IHost UseOrderSystemLogging(this IHost host)
    {
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        return host;
    }

    private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Logger.Fatal((Exception)e.ExceptionObject, "Fatal error");
        Log.CloseAndFlush();
    }

    private static void ConfigureLogging(IConfiguration configuration, string serviceName, string logsDirectory)
    {
        const string ConsoleLogTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Scope}] {Message}{NewLine}{Exception}";
        const long fileSizeLimitBytes = 52428800;
        var fileName = Path.Combine(logsDirectory, $"{serviceName}.log");

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("InstanceId", Guid.NewGuid().ToString());

        // Only add default sinks if not specified in config
        if (!configuration.GetSection("Serilog:WriteTo").Exists())
        {
            Console.WriteLine($"[WARNING] No Serilog:WriteTo configuration found for {serviceName}. Using default sinks (Seq, Console, File).");
            loggerConfig.WriteTo.Seq("http://seq:80");
            loggerConfig.WriteTo.Console(outputTemplate: ConsoleLogTemplate);
            loggerConfig.WriteTo.File(
                path: fileName,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollingInterval: RollingInterval.Day,
                formatter: new CompactJsonFormatter());
        }

        loggerConfig.ReadFrom.Configuration(configuration);
        Log.Logger = loggerConfig.CreateLogger();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
    }
}
