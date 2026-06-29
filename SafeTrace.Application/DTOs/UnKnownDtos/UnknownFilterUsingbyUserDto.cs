using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class UnknownFilterUsingbyUserDto
    {
        public string? FullName { get; set; }

        public Gender? Gender { get; set; }

        public AgeCategoryEnum? AgeCategory { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
