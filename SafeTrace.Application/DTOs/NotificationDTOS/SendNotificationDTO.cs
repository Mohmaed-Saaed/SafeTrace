using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.NotificationDTOS
{
    public class SendNotificationDTO
    {
        public string UserId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public NotificationType Type { get; set; }
    }
}
