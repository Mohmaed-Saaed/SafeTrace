namespace SafeTrace.Application.Models.Facebook
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

    public sealed class FacebookPostMediaDto
    {
        public string? FacebookMediaId { get; init; }
        public string FileUrl { get; init; } = null!;
    }
}
