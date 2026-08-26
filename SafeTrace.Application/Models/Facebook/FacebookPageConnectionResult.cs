namespace SafeTrace.Application.Models.Facebook
{
    /// <summary>
    /// Internal result returned after the Graph API validates access to a page.
    /// This type must never be returned from an API endpoint.
    /// </summary>
    public sealed class FacebookPageConnectionResult
    {
        public string FacebookPageId { get; init; } = null!;
        public string PageAccessToken { get; init; } = null!;
        public DateTimeOffset? TokenExpiresAt { get; init; }
    }
}
