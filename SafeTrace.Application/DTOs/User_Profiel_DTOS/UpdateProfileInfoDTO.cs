using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class UpdateProfileInfoDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? HomeLocation { get; set; }
        public IFormFile? IdentificationImage { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
