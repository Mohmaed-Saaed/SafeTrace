namespace SafeTrace.Domain.Entities
{
    public class FacebookImportedPostFile
    {
        public long Id { get; set; }
        public long FacebookImportedPostId { get; set; }
        public FacebookImportedPost FacebookImportedPost { get; set; } = null!;
        public string? FacebookMediaId { get; set; }
        public string FileUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
