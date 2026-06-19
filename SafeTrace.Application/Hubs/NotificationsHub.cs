using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Application.Hubs
{
    public class NotificationsHub : Hub
    {
        private readonly INotificationServices _notificationService;
        private readonly INotificationRepository _notificationRepo;

        public NotificationsHub(
            INotificationServices notificationService,
            INotificationRepository notificationRepo)
        {
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;
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
