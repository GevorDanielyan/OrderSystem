using Microsoft.AspNetCore.SignalR;

namespace OrderSystem.NotificationService;
public class NotificationHub : Hub
{
    public const string NotificationHubPath = "/ws-notifications";
    public const string SendNotificationMethodName = "SendNotification";
}
