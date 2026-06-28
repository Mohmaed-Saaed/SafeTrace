using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Message
{
    public class MessageDto
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public string? Content { get; set; }
        public FileType? FileType { get; set; }
        public string? FilePath { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeletedForEveryone { get; set; }

        public bool DeletedBySender { get; set; }

        public bool DeletedByReceiver { get; set; }

        public DateTime? ForEveryoneDeletedAt { get; set; } = null!;

        public DateTime? SenderDeletedAt { get; set; }

        public DateTime? ReceiverDeletedAt { get; set; }

        public DateTime SendAt { get; set; }
    }
}
