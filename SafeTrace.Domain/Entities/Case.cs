using SafeTrace.Domain.Enums;

namespace SafeTrace.Domain.Entities
{
    public abstract class Case
    {
        public long Id { get; set; }
        public Gender Gender { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? CommunicationPhone{ get; set; }
        public string CaseCode { get; set; } = null!;
        public CaseStatus Status { get; set; }
        public RelationType Relation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } 
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        public CaseStatus? PreviousStatus { get; set; }
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        public CaseType CaseType { get; set; }
        public int AgeCategoryId { get; set; }
        public FoundPersonInfo? FoundPersonInfo { get; set; }
        public AgeCategory AgeCategory { get; set; } = null!;
        public ICollection<CasePhoto> Photos { get; set; } = new List<CasePhoto>();
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    }
}
