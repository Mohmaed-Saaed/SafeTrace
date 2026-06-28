namespace SafeTrace.Application.DTOs.UrgentMissingCase.Response 
{
    public class UrgentCaseDetailDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public Gender Gender { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string FName { get; set; } = null!;
        public string SName { get; set; } = null!;
        public string TName { get; set; } = null!;
        public string LName { get; set; } = null!;
        public int Age { get; set; }
        public string CommunicationPhone { get; set; } = null!;
        public RelationType Relation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        public CaseStatus Status { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public AgeCategoryDto? AgeCategory { get; set; }
        public UserDto? User { get; set; }
        public List<CasePhotoDto> Photos { get; set; } = [];
    }
}
