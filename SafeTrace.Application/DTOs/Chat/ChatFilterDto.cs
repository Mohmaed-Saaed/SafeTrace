using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class ChatFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool? IsDeletedBySender { get; set; }
        public bool? IsDeletedByReceiver { get; set; }

        public string? Search { get; set; } // optional (last message / case / etc)
    }
}
