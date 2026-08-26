namespace SafeTrace.Domain.Entities
{
    public class FacebookImportedPost
    {
        public long Id { get; set; }

        public long FacebookPageId { get; set; }
        public FacebookPage FacebookPage { get; set; } = null!;
        public string FacebookPostId { get; set; } = null!;
        public string? PostText { get; set; }
        public string? PostUrl { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }

        public SocialPostClassification? Classification { get; set; }
        public double? Confidence { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public Gender? Gender { get; set; }
        public int? Age { get; set; }
        public string? Government { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public DateOnly? EventDate { get; set; }
        public string? CommunicationPhone { get; set; }
        public string? Description { get; set; }
        public RelationType? Relation { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public LocationAccuracy? LocationAccuracy { get; set; }

        public FacebookImportedPostStatus Status { get; set; }
        public string? ReviewNotes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? AnalyzedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<FacebookImportedPostFile> Files { get; set; }
            = new List<FacebookImportedPostFile>();
    }
}
