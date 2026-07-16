using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.AiMatching.Request
{
    public class AiMatchingDto
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
    }
}