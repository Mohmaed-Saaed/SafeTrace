using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.Dashboard.Response
{
    public class CaseReportDto
    {
        public long Id { get; set; }

        public string CaseCode { get; set; } = null!;

        public CaseType CaseType { get; set; }

        public CaseStatus Status { get; set; }

        public string FullName { get; set; } = null!;

        public Gender Gender { get; set; }

        public int Age { get; set; }

        public string City { get; set; } = null!;

        public string Government { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
