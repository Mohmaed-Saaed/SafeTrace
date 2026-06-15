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
        public int Founded { get; set; }
        public int Closed { get; set; }
    }
}
