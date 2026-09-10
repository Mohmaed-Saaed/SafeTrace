namespace SafeTrace.Domain.Entities
{
    public class Chat
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public long CaseId { get; set; }

        public string SenderId { get; set; } = null!;

        public ApplicationUser Sender { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public ApplicationUser Receiver { get; set; } = null!;

        public bool DeletedBySender { get; set; } = false;

        public bool DeletedByReceiver { get; set; } = false;

        public DateTime? SenderDeletedAt { get; set; }

        public DateTime? ReceiverDeletedAt { get; set; }

        public Case Case { get; set; } = null!;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
