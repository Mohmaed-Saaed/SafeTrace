using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User.Request
{
    public class UserFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public VerificationStatus? VerificationStatus { get; set; }
        public string? RoleId { get; set; }
    }
}