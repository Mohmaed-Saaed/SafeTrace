using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class AdminChatsDto
    {
        public long ChatId { get; set; }
        public long CaseId { get; set; }

        public string SenderId { get; set; }
        public string ReceiverId { get; set; }

        public int MessagesCount { get; set; }
        public int UnreadMessagesCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? LastMessage { get; set; }

        // Soft delete info (لو موجودة عندك في entity)
        public bool IsDeletedBySender { get; set; }
        public bool IsDeletedByReceiver { get; set; }

        public DateTime? SenderDeletedAt { get; set; }
        public DateTime? ReceiverDeletedAt { get; set; }
    }
}
