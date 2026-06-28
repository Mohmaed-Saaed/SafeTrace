using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class ChatDetailsDto
    {
        public long ChatId { get; set; }
        public long CaseId { get; set; }
        public string CaseTitle { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public string? SenderName { get; set; }

        public string ReceiverId { get; set; } = null!;
        public string? ReceiverName { get; set; }

        public string? OtherUserName { get; set; }

        public DateTime CreatedAt { get; set; }

        // تظهر للأدمن فقط
        public bool? DeletedBySender { get; set; }
        public bool? DeletedByReceiver { get; set; }

        public DateTime? SenderDeletedAt { get; set; }
        public DateTime? ReceiverDeletedAt { get; set; }
    }
}
