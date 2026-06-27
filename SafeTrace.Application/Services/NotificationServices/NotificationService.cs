using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Hubs;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
namespace SafeTrace.Application.Services.NotificationServices
{


    public class NotificationService : INotificationServices
    {
        private readonly IUnitOfWork _UNIT;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationsHub> hubContext,
            IMapper mapper,
            IUnitOfWork UNIT,
            ILogger<NotificationService> logger


            )
        {
            _UNIT = UNIT;
            _hubContext = hubContext;
            _mapper = mapper;
            _logger = logger;

        }


        public async Task SendNotificationAsync(SendNotificationDTO dto)
        {
            _logger.LogInformation("Sending notification to UserId: {UserId} at {date}", dto.UserId, DateTime.UtcNow);

            var notification = _mapper.Map<Notification>(dto);

            await _UNIT.Repository<Notification>().CreateAsync(notification);
            await _UNIT.SaveAsync();


            var unreadCount = await _UNIT.Repository<Notification>()
               .Query(tracked: false)
               .CountAsync(n => n.UserId == dto.UserId && !n.IsRead);

            var responseDto = _mapper.Map<GetUserNotificationsDTO>(notification);
            responseDto.CreatedAt = DateTime.UtcNow;
            responseDto.IsRead = false;

            var payload = new
            {
                Notification = responseDto,
                UnreadCount = unreadCount
            };
            _logger.LogWarning(
    "Sending notification to group: user_{UserId}",
    dto.UserId);
            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("ReceiveNotification", responseDto);

            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("UnreadCount", unreadCount);

            _logger.LogInformation("Notification sent to UserId: {UserId}. UnreadCount: {Count}", dto.UserId, unreadCount);
        }


        #region Send Notificaion To All Users
        //    private async Task<Notification> CreateNotificationAsync(
        //SendNotificationDTO dto)
        //    {
        //        var notification = _mapper.Map<Notification>(dto);

        //        notification.IsRead = false;
        //        notification.CreatedAt = DateTime.UtcNow;

        //        await _UNIT.Repository<Notification>()
        //                   .CreateAsync(notification);

        //        await _UNIT.SaveAsync();

        //        return notification;
        //    }
        //public async Task SendNotificationToAllAsync(SendToAllNotificationDTO dto)
        //{
        //    _logger.LogInformation("Broadcasting notification to all users");

        //    // الحصول على كل المستخدمين
        //    var users = await _userManager.Users.ToListAsync();

        //    var notifications = users.Select(user => new Notification
        //    {
        //        UserId = user.Id,
        //        Content = dto.Content,
        //        Type = dto.Type,
        //        IsRead = false,
        //        CreatedAt = DateTime.UtcNow
        //    }).ToList();

        //    // حفظ الإشعارات
        //    await _UNIT.Repository<Notification>().CreateRangeAsync(notifications);
        //    await _UNIT.SaveAsync();

        //    // إرسال الإشعار للمستخدمين المتصلين
        //    var responseDto = new GetUserNotificationsDTO
        //    {
        //        Content = dto.Content,
        //        Type = dto.Type,
        //        IsRead = false,
        //        CreatedAt = notifications[0].CreatedAt
        //    };

        //    await _hubContext.Clients.All.SendAsync("ReceiveNotification", responseDto);

        //    _logger.LogInformation("Broadcast notification sent successfully");
        //}
        #endregion

        public async Task<ApiResponse<int>> GetUnreadCountAsync(string userId)
        {
            var UnReadCount = await _UNIT.Repository<Notification>()
                  .Query(false)
                  .CountAsync(n => n.UserId == userId && !n.IsRead);
            return ApiResponse<int>.Ok(UnReadCount, "عدد الاشعارات غير المقرؤة.");
        }
        public async Task<ApiResponse<bool>> RemoveNotificationAsync(long id)
        {
            _logger.LogInformation("Removing notification. NotificationId: {Id} at: {date} ", id, DateTime.UtcNow);
            var notification = await _UNIT.Repository<Notification>().GetByIdAsync(id);
            if (notification is null)
            {
                _logger.LogWarning("Notification not found. NotificationId: {Id}  at: {date} ", id, DateTime.UtcNow);
                throw new NotFoundException("هذا الاشعار غير موجود");
            }
            _UNIT.Repository<Notification>().Remove(notification);
            await _UNIT.SaveAsync();

            _logger.LogInformation("Notification removed. NotificationId: {Id} at: {date} ", id, DateTime.UtcNow);
            return ApiResponse<bool>.Ok(true, "تم حذف الاشعار .");
        }

        public async Task MarkAsReadAsync(long id)
        {
            _logger.LogInformation("Marking notification as read. NotificationId: {Id} at: {date} ", id, DateTime.UtcNow);


            var affected = await _UNIT.Repository<Notification>()
            .Query()
            .Where(n => n.Id == id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            if (affected == 0)
            {
                _logger.LogWarning("Notification not found or already read. NotificationId: {Id}", id);
                throw new NotFoundException($"هذا الاشعار غير موجوداو هي مقرؤة بالفعل.");
            }
            //await _UNIT.SaveAsync();
            _logger.LogInformation("Notification marked as read. NotificationId: {Id}", id);

        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            _logger.LogInformation("Marking all notifications as read for UserId: {UserId} at: {date} ", userId, DateTime.UtcNow);

            var affected = await _UNIT.Repository<Notification>()
                     .Query()
                      .Where(n => n.UserId == userId && !n.IsRead)
                      .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            if (affected > 0)
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("UnreadCount", 0);
            }
            _logger.LogInformation("Marked {Count} notifications as read for UserId: {UserId}", affected, userId);
        }

        public async Task<ApiResponse<IEnumerable<GetUserNotificationsDTO>>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching notifications for UserId: {UserId}, Page: {Page}, PageSize: {PageSize}", userId, page, pageSize);

            var notifications = await _UNIT.Repository<Notification>()
                .Query(
                    tracked: false,
                    orderBy: n => n.CreatedAt,
                    orderByDirection: OrderBy.Descending)
                .Where(n => n.UserId == userId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var res = _mapper.Map<IEnumerable<GetUserNotificationsDTO>>(notifications);
            return ApiResponse<IEnumerable<GetUserNotificationsDTO>>.Ok(res, "تم جلب الاشعارات بنجاح.");
        }


    }

}
