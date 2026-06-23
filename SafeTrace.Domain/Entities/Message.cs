using Microsoft.Extensions.Logging;
using SafeTrace.Domain.Common;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Domain.Entities
{
    public class Message
    {
        public long Id { get; set; }

        public string Content { get; set; } = null!;

        public string SenderId { get; set; } = null!;

        public ApplicationUser Sender { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public ApplicationUser Receiver { get; set; } = null!;

        public bool DeletedBySender { get; set; } = false;

        public bool DeletedByReceiver { get; set; } = false;

        public bool IsDeletedForEveryone { get; set; } = false;

        public DateTime? ForEveryoneDeletedAt { get; set; } = null!;

        public DateTime? SenderDeletedAt { get; set; }

        public DateTime? ReceiverDeletedAt { get; set; }

        public FileType? FileType { get; set; }

        public string? FilePath { get; set; }

        public bool IsRead { get; set; }

        public DateTime SendAt { get; set; }

        public long ChatId { get; set; }

        public Chat Chat { get; set; } = null!;
    }
}


