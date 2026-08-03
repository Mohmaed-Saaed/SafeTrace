using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class RejectUserDto
    {
        public string? Reason { get; set; }
    }
}
