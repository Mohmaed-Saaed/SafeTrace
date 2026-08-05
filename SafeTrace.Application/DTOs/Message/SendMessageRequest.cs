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
        [AllowedPhotoTypes(ErrorMessage = "Only JPG, JPEG, PNG, and WebP images are allowed.")]
        [MaxPhotoSize(5, ErrorMessage = "Each photo must not exceed 5 MB.")]
        [AllowedVideoTypes(ErrorMessage = "Only MP4, MOV, and WebM videos are allowed.")]
        [MaxVideoSize(50, ErrorMessage = "Each video must not exceed 50 MB.")]
        public IFormFile? File { get; set; }
    }
}
