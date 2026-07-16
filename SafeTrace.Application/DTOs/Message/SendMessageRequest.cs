using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SafeTrace.Application.DTOs.Message
{
    public class SendMessageRequest
    {
        [Required]
        public long ChatId { get; set; }

        /// <summary>Text content — required unless a file is attached.</summary>
        public string? Content { get; set; }

        // File attachment (optional)
        public IFormFile? File { get; set; }
    }
}
