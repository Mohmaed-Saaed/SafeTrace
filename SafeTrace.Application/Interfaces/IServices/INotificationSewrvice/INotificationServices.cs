using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Interfaces.IServices.INotificationSewrvice
{
    public interface INotificationServices
    {
        //test 
        Task<IEnumerable<Notification>> GetAllrNotificationsAsync();
        Task SendNotificationAsync(SendNotificationDTO dto);
        Task SendNotificationToAllAsync(SendNotificationDTO dto);
        Task<bool> RemoveNotificationAsync(long id);
        Task MarkAsReadAsync(long id);
        Task<IEnumerable<GetUserNotificationsDTO>> GetUserNotificationsAsync(string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
