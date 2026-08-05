using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class ToggleBlockDto
    {
        public string? Reason { get; set; }
    }
}
