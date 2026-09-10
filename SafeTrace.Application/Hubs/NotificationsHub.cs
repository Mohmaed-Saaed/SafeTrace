using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;

namespace SafeTrace.Application.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        private readonly INotificationServices _notificationService;
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(
         INotificationServices notificationService,
        ILogger<NotificationsHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {

            var userId = Context.UserIdentifier;
            _logger.LogInformation("Connected: UserIdentifier={UserId}, Connection={ConnectionId}", Context.UserIdentifier, Context.ConnectionId);

            if (!string.IsNullOrEmpty(userId))
            {

                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"user_{userId}");

                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);


                await Clients.Caller.SendAsync(
                    "UnreadCount",
                    unreadCount);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(
                    Context.ConnectionId,
                    $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }


    }


}
