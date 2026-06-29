using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.DTOs.LongTermCases
{
    /// <summary>
    /// "Mark as Founded" recovery details (FR-47 .. FR-50), shown on the case details page
    /// once the case status becomes <c>Found</c>.
    /// </summary>
    public class FoundPersonInfoDto
    {
        public string Description { get; set; } = null!;
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string? Street { get; set; }
        public DateTime FoundedAt { get; set; }
    }
}
