using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Dashboard.Response
{
    public class DashboardDto
    {
        public int TotalUsers { get; set; }
        public decimal TotalSumDonations { get; set; }
        public int TotalCountFailedDonations { get; set; }
        public int TotalCountSucceededDonations { get; set; }
        public int TotalCases { get; set; }
        public int TotalFoundedCases { get; set; }
        public int TotalActiveCases { get; set; }
        public int TotalClosedCases { get; set; }
        public int TotalDeletedCases { get; set; }
        public int TotalPendingCases { get; set; }

        public List<CaseTypeStatsDto> CaseTypes { get; set; } = new();
    }
}
