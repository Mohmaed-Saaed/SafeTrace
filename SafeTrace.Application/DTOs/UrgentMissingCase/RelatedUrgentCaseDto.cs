namespace SafeTrace.Application.DTOs.UrgentMissingCase{
    public class RelatedUrgentCaseDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public string? Description { get; set; }
        public double DistanceKm { get; set; }
        public string? MainPhotoUrl { get; set; }
        public CaseStatus Status { get; set; }
    }
}

