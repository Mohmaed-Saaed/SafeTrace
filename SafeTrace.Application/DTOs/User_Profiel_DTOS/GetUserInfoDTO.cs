using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class GetUserInfoDTO
    {

        ////As Full Name
        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public string? IdentificationImage { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }

        //As Home Location (in Angular )
        public double? HomeLocationLatitude { get; set; }
        public double? HomeLocationLongitude { get; set; }
        public string? ProfileImage { get; set; }

        //Reports api to get Count (Optional)
        //Founded Cases For User Count (Optional)
    }
}
