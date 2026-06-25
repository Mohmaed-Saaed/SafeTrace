using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Domain.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
namespace SafeTrace.Application.Hubs
{

    //[AllowAnonymous]
    public class NotificationsHub : Hub
    {
        private readonly INotificationServices _notificationService;
        private readonly IUnitOfWork _unit;
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(
         INotificationServices notificationService,
        IUnitOfWork unit,
        ILogger<NotificationsHub> logger)
        {
            _notificationService = notificationService;
            _unit = unit;
            _logger = logger;
        }


        //public override async Task OnConnectedAsync()
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is not null)
        //    {
        //        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        //        var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
        //        await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        //    }
        //    await base.OnConnectedAsync();
        //}

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"user_{userId}");

                var unreadCount = await _unit.Repository<Notification>()
                    .Query(false)
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                await Clients.Caller.SendAsync(
                    "UnreadCount",
                    unreadCount);
            }

            await base.OnConnectedAsync();
        }

        //public async Task JoinAsUser(string userId)
        //{
        //    try
        //    {
        //        _logger.LogInformation("JoinAsUser called with UserId: {UserId}", userId);

        //        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        //        _logger.LogInformation("Added connection {ConnectionId} to group user_{UserId}",
        //            Context.ConnectionId, userId);

        //        var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);

        //        _logger.LogInformation("Unread count for {UserId}: {Count}",
        //            userId, unreadCount);

        //        await Clients.Caller.SendAsync("UnreadCount", unreadCount);

        //        _logger.LogInformation("UnreadCount sent successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "JoinAsUser failed for UserId: {UserId}", userId);
        //        throw;
        //    }
        //}

        public async Task JoinAsUser(string userId)
        {
            try
            {
                _logger.LogInformation(
                    "JoinAsUser called with UserId: {UserId}",
                    userId);

                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"user_{userId}");

                var unreadCount = await _unit.Repository<Notification>()
                    .Query(false)
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                await Clients.Caller.SendAsync(
                    "UnreadCount",
                    unreadCount);

                _logger.LogInformation(
                    "UnreadCount sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "JoinAsUser failed for UserId: {UserId}",
                    userId);

                throw;
            }
        }





        //public override async Task OnDisconnectedAsync(Exception? exception)
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is not null)
        //        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

        //    await base.OnDisconnectedAsync(exception);
        //}


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
        //public async Task GetMyNotifications()
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is null) return;

        //    var notifications = await _notificationRepo.GetUserNotificationsAsync(userId);
        //    await Clients.Caller.SendAsync("ReceiveNotifications", notifications);
        //}
        public async Task GetMyNotifications()
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            var notifications = await _unit.Repository<Notification>()
                .Query(false,
                       orderBy: n => n.CreatedAt,
                       orderByDirection: OrderBy.Descending)
                .Where(n => n.UserId == userId)
                .ToListAsync();

            await Clients.Caller.SendAsync(
                "ReceiveNotifications",
                notifications);
        }


        //public async Task MarkAsRead(long notificationId)
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is null) return;

        //    await _notificationService.MarkAsReadAsync(notificationId);

        //    var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
        //    await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        //}


        public async Task MarkAsRead(long notificationId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            await _notificationService.MarkAsReadAsync(notificationId);

            var unreadCount = await _unit.Repository<Notification>()
                .Query(false)
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await Clients.Caller.SendAsync(
                "UnreadCount",
                unreadCount);
        }

        //public async Task MarkAllAsRead()
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is null) return;

        //    await _notificationService.MarkAllAsReadAsync(userId);
        //}
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


        //public async Task RemoveNotification(long notificationId)
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId is null) return;

        //    await _notificationService.RemoveNotificationAsync(notificationId);

        //    var unreadCount = await _notificationRepo.GetUnreadCountAsync(userId);
        //    await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        //}

        public async Task RemoveNotification(long notificationId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
                return;

            await _notificationService.RemoveNotificationAsync(notificationId);

            var unreadCount = await _unit.Repository<Notification>()
                .Query(false)
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await Clients.Caller.SendAsync(
                "UnreadCount",
                unreadCount);
        }
    }

}
//3755d80b-3da8-46d7-8029-64b4520d6b73
