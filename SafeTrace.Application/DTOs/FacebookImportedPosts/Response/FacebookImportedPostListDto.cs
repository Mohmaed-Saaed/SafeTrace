namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed class FacebookImportedPostListDto
    {
        public long Id { get; set; }
        public long FacebookPageId { get; set; }
        public string FacebookPageName { get; set; } = null!;
        public string FacebookPostId { get; set; } = null!;
        public string? PostUrl { get; set; }
        public string? PostTextPreview { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public SocialPostClassification? Classification { get; set; }
        public double? Confidence { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public int? Age { get; set; }
        public Gender? Gender { get; set; }
        public FacebookImportedPostStatus Status { get; set; }
        public DateTime? AnalyzedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<string> MissingRequirements { get; set; } = [];
    }
}
