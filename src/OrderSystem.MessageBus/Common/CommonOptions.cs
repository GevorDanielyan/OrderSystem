using Polly;
using System.Text.Json;
using System.Net.Sockets;
using RabbitMQ.Client.Exceptions;

namespace OrderSystem.MessageBus.Common;

/// <summary>
/// Message bus connection retry policy
/// </summary>
/// <param name="RetryCount">Number of retries</param>
/// <param name="SleepDurationProvider">Sleep duration logic for every retry</param>
public record ConnectionRetryPolicy(int RetryCount, Func<int, TimeSpan> SleepDurationProvider);

internal static class CommonOptions
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ConnectionRetryPolicy DefaultConnectionRetryPolicy = new(5, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

    public static Func<int, TimeSpan> DefaultInfinityRetrySleepDurationProvider = retryAttempt => 
        TimeSpan.FromMilliseconds(Math.Min(Math.Pow(2, retryAttempt) * 100, 30_000)); // Exponential growth but no more than 30 seconds

    public static PolicyBuilder ConnectionExceptionsPolicyBuilder => Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .Or<AlreadyClosedException>()
            .Or<OperationInterruptedException>()
            .Or<ConnectFailureException>();
}
