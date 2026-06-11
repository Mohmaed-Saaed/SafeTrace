using SafeTrace.Domain.Enums;

namespace SafeTrace.Domain.Entities
{
    public class Message
    {
        public long Id { get; set; }

        public string Content { get; set; } = null!;

        public string SenderId { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public FileType? FileType { get; set; }

        public string? FilePath { get; set; }

        public bool IsRead { get; set; }

        public DateTime SendAt { get; set; }

        public long ChatId { get; set; }

        public Chat Chat { get; set; } = null!;
    }
}
