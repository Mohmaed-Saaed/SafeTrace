namespace SafeTrace.Application.DTOs.UrgentMissingCase{
    public class UrgentCaseDetailDto
    {
        public long Id { get; set; }
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }
        public string? Description { get; set; }
        public string UserId { get; set; } = null!;
        public string? CommunicationPhone{ get; set; }
        public CaseStatus Status { get; set; }
        public double LocationLatitude { get; set; } 
        public double LocationLongitude { get; set; }
        public string CaseCode { get; set; } = null!;
        public RelationType Relation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LostDate { get; set; }
        public DateTime EndDate { get; set; }
        public string?  AgeCategory { get; set; }
        public List<string> Photos { get; set; } = [];
    }
}
