using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class VisitUserDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
