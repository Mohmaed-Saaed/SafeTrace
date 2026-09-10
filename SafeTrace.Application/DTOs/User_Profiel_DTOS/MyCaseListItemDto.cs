using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS
{
    public class MyCaseListItemDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public int Age { get; set; }
        public AgeCategoryDto? AgeCategory { get; set; }

        public long? FoundPersonInfoId { get; set; }

        public Gender Gender { get; set; }
        public CaseStatus Status { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public CaseType CaseType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MainImageUrl { get; set; }
    }
}
