
namespace SafeTrace.Application.DTOs.UrgentMissingCase.Response
{
    public class UrgentCaseAdminDto
    {
        public long Id { get; set; }

        public string CaseCode { get; set; } = null!;
        public CaseStatus Status { get; set; }
        public CaseStatus? PreviousStatus { get; set; }

        public string FullName { get; set; } = null!;

        public int Age { get; set; }

        public Gender Gender { get; set; }
        public RelationType Relation { get; set; }
        public CaseType CaseType { get; set; }

        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime EventDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LimitReachDate { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<CasePhotoDto> Photos { get; set; } = [];

        public FoundPersonInfoDto FoundPersonInfo { get; set; }  = null!;

        public UserDto User { get; set; } = null!;

        public AgeCategoryDto AgeCategory { get; set; } = null!;
    }
}

