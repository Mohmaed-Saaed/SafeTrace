using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class GetAllDTO
    {
        public string id;
        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; }
        public string? IdentificationImage { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public double? HomeLocationLatitude { get; set; }
        public double? HomeLocationLongitude { get; set; }
        public string? ProfileImage { get; set; }
    }
}
