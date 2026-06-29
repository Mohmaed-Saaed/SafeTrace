using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using Microsoft.AspNetCore.Authorization;

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
        #region test
        //public async Task JoinAsUser(string userId)
        //{
        //    _logger.LogWarning(
        //               "JoinAsUser => ConnectionId={ConnectionId}, UserId={UserId}",
        //               Context.ConnectionId,
        //               userId);

        //    await Groups.AddToGroupAsync(
        //        Context.ConnectionId,
        //        $"user_{userId}"); //done

        //    _logger.LogWarning(
        //        "Added To Group => user_{UserId}",
        //        userId);

        //    var unreadCount = await _unit.Repository<Notification>()
        //        .Query(false)
        //        .CountAsync(n => n.UserId == userId && !n.IsRead);//done

        //    await Clients.Caller.SendAsync(
        //        "UnreadCount",
        //        unreadCount);

        //    _logger.LogWarning(
        //        "UnreadCount Sent => {Count}",
        //        unreadCount);
        //}
        #endregion
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

        public async Task GetMyNotifications()
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            await Clients.Caller.SendAsync(
                "ReceiveNotifications",
                notifications);
        }



        public async Task MarkAsRead(long notificationId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            await _notificationService.MarkAsReadAsync(notificationId);

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            await Clients.Caller.SendAsync(
                "UnreadCount",
                unreadCount);
        }


        public async Task MarkAllAsRead()
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            await _notificationService.MarkAllAsReadAsync(userId);

            await Clients.Caller.SendAsync(
                "UnreadCount",
                0);
        }


        public async Task RemoveNotification(long notificationId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            await _notificationService.RemoveNotificationAsync(notificationId);

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);


            await Clients.Caller.SendAsync(
                "UnreadCount",
                unreadCount);
        }
    }

}
