namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Request
{
    public sealed class RejectFacebookImportedPostDto
    {
        [StringLength(2000)]
        public string? ReviewNotes { get; set; }
    }
}
