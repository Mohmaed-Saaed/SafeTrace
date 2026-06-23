using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Application.Hubs
{

    //[AllowAnonymous]
    public class NotificationsHub : Hub
    {
        private readonly INotificationServices _notificationService;
        private readonly INotificationRepository _notificationRepo;
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(
            INotificationServices notificationService,
            INotificationRepository notificationRepo,
            ILogger<NotificationsHub> logger)
        {
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId is not null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("UnreadCount", unreadCount);
            }
            await base.OnConnectedAsync();
        }

        //public override async Task OnConnectedAsync()
        //{
        //    var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

        //    _logger.LogInformation("Connected User: {UserId}", userId);

        //    if (!string.IsNullOrEmpty(userId))
        //    {
        //        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        //        _logger.LogInformation("Added to group: user_{UserId}", userId);
        //    }

        //    await base.OnConnectedAsync();
        //}
        // Add inside NotificationsHub



        public async Task JoinAsUser(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }

        //public override async Task OnDisconnectedAsync(Exception? exception)
        //{
        //    var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

        //    if (!string.IsNullOrEmpty(userId))
        //    {
        //        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        //    }

        //    await base.OnDisconnectedAsync(exception);
        //}


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId is not null)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            await base.OnDisconnectedAsync(exception);
        }

        public async Task GetMyNotifications()
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;

            var notifications = await _notificationRepo.GetUserNotificationsAsync(userId);
            await Clients.Caller.SendAsync("ReceiveNotifications", notifications);
        }

        public async Task MarkAsRead(long notificationId)
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;

            await _notificationService.MarkAsReadAsync(notificationId);

            var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }

        public async Task MarkAllAsRead()
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;

            await _notificationService.MarkAllAsReadAsync(userId);
        }

        public async Task RemoveNotification(long notificationId)
        {
            var userId = Context.UserIdentifier;
            if (userId is null) return;

            await _notificationService.RemoveNotificationAsync(notificationId);

            var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }
    }

}
//3755d80b-3da8-46d7-8029-64b4520d6b73
