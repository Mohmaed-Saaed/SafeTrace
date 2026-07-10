namespace SafeTrace.Domain.Entities
{
    public class CaseFile
    {
        public long Id { get; set; }
        public long CaseId { get; set; }
        public string ImagePath { get; set; } = null!;
        public string? FaceId { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public Case Case { get; set; } = null!;
    }
}
