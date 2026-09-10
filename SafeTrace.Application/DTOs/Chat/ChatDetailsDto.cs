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

        public CaseType CaseType { get; set; }
        public CaseStatus CaseStatus { get; set; }

        public string? CaseImage { get; set; }

        public long? FoundCaseId { get; set; }

        public string SenderId { get; set; } = null!;
        public string? SenderName { get; set; }
        public string? SenderImage { get; set; }

        public string ReceiverId { get; set; } = null!;
        public string? ReceiverName { get; set; }
        public string? ReceiverImage { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserImage { get; set; }

        public DateTime CreatedAt { get; set; }

        // تظهر للأدمن فقط
        public bool? DeletedBySender { get; set; }
        public bool? DeletedByReceiver { get; set; }

        public DateTime? SenderDeletedAt { get; set; }
        public DateTime? ReceiverDeletedAt { get; set; }
    }
}
