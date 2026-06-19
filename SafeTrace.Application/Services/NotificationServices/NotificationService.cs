using Microsoft.AspNetCore.SignalR;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Hubs;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Application.Services.NotificationServices
{
    public class NotificationService : INotificationServices
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly IMapper _mapper;

        public NotificationService(
            INotificationRepository notificationRepo,
            IHubContext<NotificationsHub> hubContext,
            IMapper mapper
            )
        {
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        #region test
        public async Task<IEnumerable<Notification>> GetAllrNotificationsAsync()
        {
            var notifis = await _notificationRepo.GetAllrNotificationsAsync();
            return notifis;
        }
        #endregion
        public async Task SendNotificationAsync(SendNotificationDTO dto)
        {
            var notification = _mapper.Map<Notification>(dto);
            await _notificationRepo.CreateAsync(notification);
            await _notificationRepo.SaveAsync();
            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.Id,
                    notification.Content,
                    notification.Type,
                    notification.IsRead,
                    notification.CreatedAt
                });

            var unreadCount = await _notificationRepo.GetUnreadCountAsync(dto.UserId);
            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("UnreadCount", unreadCount);
        }

        public async Task SendNotificationToAllAsync(SendNotificationDTO dto)
        {
            //var notifiction = _mapper.Map<Notification>(dto);
            //await _hubContext.Clients.All.SendAsync("ReceiveNotification", notifiction);

            // 1. Map من DTO للـ Entity عشان تحفظ
            var notification = _mapper.Map<Notification>(dto);
            notification.IsRead = false;
            notification.CreatedAt = DateTime.UtcNow;

            // 2. احفظ في الـ Database
            await _notificationRepo.CreateAsync(notification);
            await _notificationRepo.SaveAsync();

            // 3. ابعت DTO نظيف على SignalR مش Entity
            var responseDto = _mapper.Map<GetUserNotificationsDTO>(notification);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", responseDto);
        }

        public async Task<bool> RemoveNotificationAsync(long id)
        {
            var notification = await _notificationRepo.GetNotificationByIdAsync(id);
            if (notification is null) return false;
            await _notificationRepo.DeleteAsync(notification); // ← DeleteAsync
            await _notificationRepo.SaveAsync();
            return true;
        }

        public async Task MarkAsReadAsync(long id)
        {
            await _notificationRepo.MarkAsReadAsync(id);
            await _notificationRepo.SaveAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _notificationRepo.MarkAllAsReadAsync(userId);

            await _hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("UnreadCount", 0);
        }

        public async Task<IEnumerable<GetUserNotificationsDTO>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await _notificationRepo.GetUserNotificationsAsync(userId);
            return _mapper.Map<IEnumerable<GetUserNotificationsDTO>>(notifications);
        }


    }

}
