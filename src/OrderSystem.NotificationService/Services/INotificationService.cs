namespace OrderSystem.NotificationService.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string message, CancellationToken cancellationToken);
}
