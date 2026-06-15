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
    /// Search, filter, sort and pagination options for the Long-Term Cases list
    /// (FR-22 search by name, FR-23 filter by gender, FR-24 filter by age category,
    /// FR-25 sort by date).
    /// </summary>
    public class LongTermCaseFilterDto
    {
        /// <summary>Search by name (FR-22) - matches FName/SName/TName/LName.</summary>
        public string? Name { get; set; }

        /// <summary>Filter by gender (FR-23).</summary>
        public Gender? Gender { get; set; }

        /// <summary>Filter by age category (FR-24).</summary>
        public AgeCategory? AgeCategory { get; set; }

        /// <summary>Sort by date (FR-25). true = newest first (default).</summary>
        public bool SortDescending { get; set; } = true;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
