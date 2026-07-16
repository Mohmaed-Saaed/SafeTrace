using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Dashboard.Response
{
    public class CaseTypeStatsDto
    {
        public string CaseType { get; set; } = string.Empty;

        public int Total { get; set; }
        public int Active { get; set; }
        public int Deleted { get; set; }
        public int Pending { get; set; }
        public int Found { get; set; }
        public int Rejected { get; set; }
        public int Expired { get; set; }
    }
}
