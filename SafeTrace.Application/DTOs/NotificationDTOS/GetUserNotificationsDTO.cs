using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.NotificationDTOS
{
    public class GetUserNotificationsDTO
    {
        public long Id { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public NotificationType Type { get; set; }

        public string Content { get; set; } = null!;

        public string? NotificationDirectLink { get; set; }

    }
}
