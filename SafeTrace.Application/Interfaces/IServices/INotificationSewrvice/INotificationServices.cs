using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Interfaces.IServices.INotificationSewrvice
{
    public interface INotificationServices
    {
        //test 
        //Task<ApiResponse<IEnumerable<Notification>>> GetAllrNotificationsAsync();
        Task SendNotificationAsync(SendNotificationDTO dto);
        //Task SendNotificationToAllAsync(SendToAllNotificationDTO dto);
        Task<ApiResponse<bool>> RemoveNotificationAsync(long id);
        Task<ApiResponse<int>> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(long id);
        Task<ApiResponse<NotificationPageDto>> GetUserNotificationsAsync(
          string userId,
          int page = 1,
          int pageSize = 10);
        Task MarkAllAsReadAsync(string userId);
    }
}
