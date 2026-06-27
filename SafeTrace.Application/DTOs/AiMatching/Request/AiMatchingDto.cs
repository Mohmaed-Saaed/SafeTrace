using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.AiMatching.Request
{
    public class AiMatchingDto
    {
        public IFormFile Image { get; set; } = null!;
    }
}