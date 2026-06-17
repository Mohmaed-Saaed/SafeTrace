using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class ChatDetailsDto
    {
        public long ChatId { get; set; }
        public long CaseId { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
