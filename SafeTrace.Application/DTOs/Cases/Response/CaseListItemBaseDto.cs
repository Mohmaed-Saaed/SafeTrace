namespace SafeTrace.Application.DTOs.Cases.Response
{
    public abstract class CaseListItemBaseDto
    {
        public long Id { get; set; }
        public string CaseCode { get; set; } = null!;
        public CaseType CaseType { get; set; }
        public CaseStatus Status { get; set; }

        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }

        public Gender Gender { get; set; }
        public int Age { get; set; }

        public string City { get; set; } = null!;
        public string Government { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public string MainPhoto { get; set; } = null!;
    }
}