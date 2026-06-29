using SafeTrace.Domain.Enums;


namespace SafeTrace.Application.DTOs.LongTermCases
{
    /// <summary>
    /// Represents the "Case Card" shown in lists / search results (FR-21):
    /// Photo, Name, Age, Gender, Age Category, Missing Date.
    /// </summary>
    public class LongTermCaseCardDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public string? FullName { get; set; }
        public int Age { get; set; }
        public AgeCategoryEnum AgeCategory { get; set; }
        public Gender Gender { get; set; }

        /// <summary>Maps to BaseCase.CreatedAt (the date the case was reported/missing).</summary>
        public DateTime MissingDate { get; set; }

        /// <summary>First photo of the case, if any.</summary>
        public string? MainPhoto { get; set; }

        public CaseStatus Status { get; set; }
    }
}
