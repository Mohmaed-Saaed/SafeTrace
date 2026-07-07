using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class StartChatContextDto
    {
        public long CaseId { get; set; }

        public string CaseTitle { get; set; } = null!;

        public string? CaseImage { get; set; }

        public string Status { get; set; } = null!;

        public string ParticipantName { get; set; } = null!;

        public string? ParticipantImage { get; set; }

        public bool ChatExists { get; set; }

        public long? ChatId { get; set; }
    }
}
