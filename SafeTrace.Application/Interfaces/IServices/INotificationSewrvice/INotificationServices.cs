using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Interfaces.IServices.INotificationSewrvice
{
    public interface INotificationServices
    {
        //test 
        Task<IEnumerable<Notification>> GetAllrNotificationsAsync();
        Task SendNotificationAsync(SendNotificationDTO dto);
        //Task SendNotificationToAllAsync(SendToAllNotificationDTO dto);
        Task<bool> RemoveNotificationAsync(long id);
        Task MarkAsReadAsync(long id);
        //Task<IEnumerable<GetUserNotificationsDTO>> GetUserNotificationsAsync(string userId);
        Task<IEnumerable<GetUserNotificationsDTO>> GetUserNotificationsAsync(
          string userId,
          int page = 1,
          int pageSize = 10);
        Task MarkAllAsReadAsync(string userId);
    }
}
