using SafeTrace.Application.Common.Enums;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.DTOs.LongTermCases
{
    /// <summary>
    /// Full details of a Long-Term Missing Case, used on the case details page.
    /// </summary>
    public class LongTermCaseDetailsDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;

        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public string? FullName { get; set; }

        public Gender Gender { get; set; }
        public int Age { get; set; }
        public AgeCategory AgeCategory { get; set; }
        public RelationType Relation { get; set; }
        public CaseStatus Status { get; set; }



        // Last known location of the missing person (LongTermMissingCaseConfiguration)
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string? Street { get; set; }

        public string? Description { get; set; }
        public string? PoliceReportImage { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<CasePhotoDto> Photos { get; set; } = new();

        /// <summary>Populated only when Status == Found (FR-50).</summary>
        public FoundPersonInfoDto? FoundPersonInfo { get; set; }

        // Basic reporter info - used to start a chat / show "reported by" (FR-39)
        public string ReporterId { get; set; } = null!;
        public string? ReporterUserName { get; set; }
    }
}
