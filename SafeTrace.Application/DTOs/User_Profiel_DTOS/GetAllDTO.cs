using System;
using System.Collections.Generic;
using System.Text;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class GetAllDTO
    {
        public string Id;
        public string FullName { get; set; } = string.Empty;

        public string? IdentificationImageFront { get; set; }
        public string? IdentificationImageBack { get; set; }
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }

        //As Home Location (in Angular )
        public string? HomeLocation { get; set; }
        public string? ProfileImage { get; set; } = "لم يتم الاضافة";
    }
}
