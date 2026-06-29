namespace SafeTrace.Application.DTOs.UrgentMissingCase.Response
{
    public class UrgentCaseListItemDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public Gender Gender { get; set; }
        public int Age { get; set; }
        public string City { get; set; } = null!;
        public string Government { get; set; } = null!;
        public CaseStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LimitReachDate { get; set; } 
        public string MainPhoto { get; set; } = null!;
    }
}

