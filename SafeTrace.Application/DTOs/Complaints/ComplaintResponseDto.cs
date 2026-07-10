using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Complaints
{
    public class ComplaintResponseDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
         public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}