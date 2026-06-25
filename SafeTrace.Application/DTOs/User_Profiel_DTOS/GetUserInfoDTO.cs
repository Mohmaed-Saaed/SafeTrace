using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class GetUserInfoDTO
    {

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? HomeLocation { get; set; }
        public string? ProfileImage { get; set; }
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
        public string? IdentificationImage { get; set; }

        //As Home Location (in Angular )

        //Reports api to get Count (Optional)
        //Founded Cases For User Count (Optional)
    }
}
//"email": "faroukyousef0@gmail.com",
// "password": "Aa@12345678"
//19482e1b-ff37-4f18-9408-5f2db81b8771