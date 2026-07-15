using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Dashboard.Response
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCases { get; set; }
        public int ActiveCases { get; set; }
        public int FoundCases { get; set; }
        public int PendingCases { get; set; }
        public int ClosedCases { get; set; }
    }
}
