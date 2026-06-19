using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.NotificationDTOS
{
    public class GetUserNotificationsDTO
    {
        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public NotificationType Type { get; set; }

        public string UserId { get; set; } = null!;

        public string Content { get; set; } = null!;

    }
}
