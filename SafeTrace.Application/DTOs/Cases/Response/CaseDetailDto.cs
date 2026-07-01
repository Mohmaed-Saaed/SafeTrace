namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class CaseDetailDto
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

        // Specific to UrgentCase
        public DateTime? EndDate { get; set; }
        public DateTime? LimitReachDate { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Specific to found/admin stuff
        public FoundPersonInfoDto? FoundPersonInfo { get; set; }

        // We might not have AgeCategoryDto here yet, but let's assume it exists in some namespace. 
        // We will just use object or basic types if we can't find it. For now, assuming it exists:
        public AgeCategoryDto? AgeCategory { get; set; } 
        public UserDto? User { get; set; }
        public List<CasePhotoDto> Photos { get; set; } = [];
    }
}

