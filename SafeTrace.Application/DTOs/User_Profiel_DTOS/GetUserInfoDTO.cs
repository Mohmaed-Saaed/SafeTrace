using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class GetUserInfoDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public double? HomeLatitude { get; set; }
        public double? HomeLongitude { get; set; }
        public string? ProfileImage { get; set; }
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
        public string? IdentificationImage { get; set; }

        public string Role { get; set; } = string.Empty;
        public ICollection<MyCaseListItemDto> Cases { get; set; }
            = new List<MyCaseListItemDto>();
    }
}
//"email": "faroukyousef0@gmail.com",
// "password": "Aa@12345678"
//"90220b70-2c58-4ba1-8b3d-10e00f224d1a",

