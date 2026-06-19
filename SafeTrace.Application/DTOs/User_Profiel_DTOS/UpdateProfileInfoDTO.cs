using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class UpdateProfileInfoDTO
    {
        ////As Full Name

        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string Email { get; set; } = string.Empty;

        //As Home Location (in Angular )
        public double? HomeLocationLatitude { get; set; }
        public double? HomeLocationLongitude { get; set; }
        public string? ProfileImage { get; set; }
    }
}
