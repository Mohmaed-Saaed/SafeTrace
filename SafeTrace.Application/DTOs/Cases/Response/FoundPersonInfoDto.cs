namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class FoundPersonInfoDto
    {
        public long Id { get; set; }
        public string Description { get; set; } = null!;
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public long CaseId { get; set; }
        public DateOnly FoundedAt { get; set; }
        public string FoundedUserId { get; set; } = null!;
    }
}

