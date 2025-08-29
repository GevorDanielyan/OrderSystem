using Microsoft.AspNetCore.SignalR.Client;

namespace OrderSystem.ConsoleClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5003/ws-notifications")
                .Build();

            connection.On<string>("SendNotification", message =>
            {
                Console.WriteLine($"Received notification: {message}");
            });

            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connected to NotificationHub. Waiting for notifications...");
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await connection.StopAsync();
            }
        }
    }
}
