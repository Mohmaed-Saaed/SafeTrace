namespace SafeTrace.Application.DTOs.Cases.Response
{
    public abstract class CaseDetailBaseDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public CaseType CaseType { get; set; }
        public CaseStatus Status { get; set; }

        public Gender Gender { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;

        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }

        public string? CommunicationPhone { get; set; }
        public RelationType Relation { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }

        public FoundPersonInfoDto? FoundPersonInfo { get; set; }
        public AgeCategoryDto? AgeCategory { get; set; }
        public UserDto? User { get; set; }
        public List<CasePhotoDto> Photos { get; set; } = [];
    }
}