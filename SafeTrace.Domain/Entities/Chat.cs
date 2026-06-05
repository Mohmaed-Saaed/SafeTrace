using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Domain.Entities
{
    public class Chat
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public long CaseId { get; set; }

        public string SenderId { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public BaseCase Case { get; set; } = null!;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
