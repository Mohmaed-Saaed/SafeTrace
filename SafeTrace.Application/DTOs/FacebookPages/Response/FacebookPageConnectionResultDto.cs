namespace SafeTrace.Application.DTOs.FacebookPages.Response
{
    /// <summary>
    /// نتيجة داخلية لربط صفحة Facebook. لا تُعاد من أي API endpoint لأن بها token سري.
    /// </summary>
    public sealed class FacebookPageConnectionResultDto
    {
        public string FacebookPageId { get; init; } = null!;
        public string PageAccessToken { get; init; } = null!;
        public DateTimeOffset? TokenExpiresAt { get; init; }
    }
}
