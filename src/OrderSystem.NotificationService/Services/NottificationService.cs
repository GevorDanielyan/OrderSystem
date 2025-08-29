using Microsoft.AspNetCore.SignalR;

namespace OrderSystem.NotificationService.Services;

internal sealed class NottificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NottificationService> _logger;

    public NottificationService(IHubContext<NotificationHub> hubContext, ILogger<NottificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending notification: {Message}", message);
        try
        {
            await _hubContext.Clients.All.SendAsync(NotificationHub.SendNotificationMethodName, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification: {Message}", message);
        }
    }
}
