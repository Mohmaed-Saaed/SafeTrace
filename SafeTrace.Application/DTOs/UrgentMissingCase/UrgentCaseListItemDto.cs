namespace SafeTrace.Application.DTOs.UrgentMissingCase{
    public class UrgentCaseListItemDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public CaseStatus Status { get; set; }
        public DateTime LostDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MainPhotoUrl { get; set; }
    }
}

