using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User.Response
{
    public class GetUserByIdDto
    {
        public string Id { get; set; } = null!;
        public string FName { get; set; } = null!;
        public string LName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? ProfileImage { get; set; }
        public string? IdentificationImageFront { get; set; }
        public string? IdentificationImageBack { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public bool IsBlocked { get; set; }
        public string Role { get; set; } = null!;
    }
}