using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class ChatSummaryDto
    {
        public long ChatId { get; set; }
        public long CaseId { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
        public int UnreadCount { get; set; }

        /// <summary>The other participant's id (not the caller).</summary>
        public string OtherUserId { get; set; } = null!;
    }
}
