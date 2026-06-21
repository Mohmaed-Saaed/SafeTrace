using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Interfaces.IServices;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class UpdateProfileInfoDTO
    {
        public string FName { get; set; } = string.Empty;
        public string LName { get; set; } = string.Empty;

        public bool IsVerified { get; set; }
        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }
        public double? HomeLocationLatitude { get; set; }
        public double? HomeLocationLongitude { get; set; }
        public IFormFile? IdentificationImage { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }
}
