using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Founded.Response
{
    public class FoundPersonListItemDto
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public Gender Gender { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; } 
        public DateOnly FoundedAt { get; set; }
        public string Image { get; set; } = null!;
    }
}
