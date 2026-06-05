using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class Notification
    {
        public long Id { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public NotificationType Type { get; set; }

        public string UserId { get; set; } = null!;

        public string Content { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}
