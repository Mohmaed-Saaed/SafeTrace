using System;
using System.Collections.Generic;
using System.Text;


namespace SafeTrace.Application.DTOs.Complaints
{
    public class ComplaintFilterDto
    {
        public string? UserId { get; set; }
        public string? CaseCode { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
