namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed class FacebookImportedPostFileDto
    {
        public long Id { get; set; }
        public string FileUrl { get; set; } = null!;
    }
}
