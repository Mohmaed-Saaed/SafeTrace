using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class GetUnknownDto
    {

        public Gender Gender { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }

        public string Government { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Street { get; set; } = null!;
        public string? CommunicationPhone { get; set; }
        public string? Description { get; set; }

        public List<UnknownPhotoDto> Photos { get; set; } = new();
        public AgeCategoryEnum AgeCategory { get; set; }
    }
}
