using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class UnKnownCaseFilterDto
    {
        public string? Name { get; set; }
        public Gender? Gender { get; set; }
        public int? AgeCategoryId { get; set; }

        public string? SortDirection { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}

