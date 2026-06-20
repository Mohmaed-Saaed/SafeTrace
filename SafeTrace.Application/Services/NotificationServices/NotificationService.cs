using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.Extensions;
using SafeTrace.Application.Hubs;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Application.Services.NotificationServices
{
    public class NotificationService : INotificationServices
    {
        private readonly IUnitOfWork _UNIT;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly IMapper _mapper;

        public NotificationService(
            IHubContext<NotificationsHub> hubContext,
            IMapper mapper,
            IUnitOfWork UNIT
            )
        {
            _UNIT = UNIT;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        #region test
        public async Task<IEnumerable<Notification>> GetAllrNotificationsAsync()
        {
            var notifications = await _UNIT.NotificationRepository

    .Query(
        tracked: false)
    .ToListAsync();
            return notifications;
        }
        #endregion



        public async Task SendNotificationAsync(SendNotificationDTO dto)
        {
            var notification = _mapper.Map<Notification>(dto);
            await _UNIT.NotificationRepository.CreateAsync(notification);
            await _UNIT.SaveAsync();
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

            var unreadCount = await _UNIT.NotificationRepository
            .Query(tracked: false)
            .CountAsync(n => n.UserId == dto.UserId && !n.IsRead);
            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("UnreadCount", unreadCount);
        }

        public async Task SendNotificationToAllAsync(SendNotificationDTO dto)
        {

            // 1. Map من DTO للـ Entity عشان تحفظ
            var notification = _mapper.Map<Notification>(dto);
            notification.IsRead = false;
            notification.CreatedAt = DateTime.UtcNow;

            // 2. احفظ في الـ Database
            await _UNIT.NotificationRepository.CreateAsync(notification);
            await _UNIT.SaveAsync();

            // 3. ابعت DTO نظيف على SignalR مش Entity
            var responseDto = _mapper.Map<GetUserNotificationsDTO>(notification);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", responseDto);
        }

        public async Task<bool> RemoveNotificationAsync(long id)
        {
            var notification = await _UNIT.NotificationRepository.GetByIdAsync(id);
            if (notification is null) return false;
            _UNIT.NotificationRepository.Remove(notification);
            await _UNIT.SaveAsync();
            return true;
        }

        public async Task MarkAsReadAsync(long id)
        {
            var notification = await _UNIT.NotificationRepository.GetByIdAsync(id);

            if (notification is null)
                return;
            notification.IsRead = true;

            _UNIT.NotificationRepository.Update(notification);

            await _UNIT.SaveAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _UNIT.NotificationRepository
                     .Query()
                      .Where(n => n.UserId == userId && !n.IsRead)
                      .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            await _hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("UnreadCount", 0);
        }

        public async Task<IEnumerable<GetUserNotificationsDTO>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await _UNIT.NotificationRepository
    .Query(
        tracked: false,
        orderBy: n => n.CreatedAt,
        orderByDirection: OrderBy.Descending)
        .Where(n => n.UserId == userId)
        .ToListAsync();
            return _mapper.Map<IEnumerable<GetUserNotificationsDTO>>(notifications);
        }


    }

}
