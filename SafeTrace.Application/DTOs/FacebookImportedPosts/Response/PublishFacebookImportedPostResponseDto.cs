namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed class PublishFacebookImportedPostResponseDto
    {
        public long FacebookImportedPostId { get; set; }
        public long CaseId { get; set; }
        public string CaseCode { get; set; } = null!;
        public CaseType CaseType { get; set; }
        public FacebookImportedPostStatus Status { get; set; }
    }
}
