using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Complaints
{
    public class CreateComplaintDto
    {
        public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
    }
}
