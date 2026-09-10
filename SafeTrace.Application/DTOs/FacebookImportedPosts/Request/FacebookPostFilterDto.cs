namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Request
{
    public sealed class FacebookPostFilterDto
    {
        public FacebookImportedPostStatus? Status { get; set; }
        public SocialPostClassification? Classification { get; set; }
        public long? FacebookPageId { get; set; }
    }
}
