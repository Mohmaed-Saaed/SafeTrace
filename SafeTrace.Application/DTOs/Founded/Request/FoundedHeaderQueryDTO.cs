using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Founded.Request
{
    public class FoundedHeaderQueryDTO
    {
        public string? Search { get; set; }
        public int AgeCategory { get; set; }
        public CaseType? CaseType { get; set; }
        public Gender? Gender { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
