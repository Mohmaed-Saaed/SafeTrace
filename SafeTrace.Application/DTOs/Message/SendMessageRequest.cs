using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.Message
{
    public class SendMessageRequest
    {
        [Required]
        public long ChatId { get; set; }

        /// <summary>Text content — required unless a file is attached.</summary>
        public string? Content { get; set; }

        // File attachment (optional)
        [AllowedMessageFileTypes(ErrorMessage = "Only JPG, JPEG, PNG, WebP, MP4, MOV, and WebM files are allowed.")]
        [MaxMessageFileSize]
        public IFormFile? File { get; set; }
    }
}
