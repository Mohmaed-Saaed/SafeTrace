using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SafeTrace.Application.DTOs.Chat
{
    public class StartChatRequest
    {
        [Required]
        public long CaseId { get; set; }
    }
}
