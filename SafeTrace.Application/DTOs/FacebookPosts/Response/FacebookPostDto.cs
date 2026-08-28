namespace SafeTrace.Application.DTOs.FacebookPosts.Response
{
    public sealed class FacebookPostDto
    {
        public string FacebookPostId { get; init; } = null!;
        public string? PostText { get; init; }
        public string? PostUrl { get; init; }
        public DateTimeOffset? PublishedAt { get; init; }
        public IReadOnlyList<FacebookPostMediaDto> Media { get; init; }
            = Array.Empty<FacebookPostMediaDto>();
    }
}
