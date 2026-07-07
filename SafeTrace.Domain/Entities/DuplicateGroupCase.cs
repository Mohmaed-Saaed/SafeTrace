namespace SafeTrace.Domain.Entities
{
    public class DuplicateGroupCase
    {
        public long Id { get; set; }
        public long DuplicateGroupId { get; set; }
        public long CaseId { get; set; }
        public decimal SimilarityScore { get; set; }
        public DuplicateMatchType MatchedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DuplicateGroup DuplicateGroup { get; set; } = null!;
        public Case Case { get; set; } = null!;
    }
}
