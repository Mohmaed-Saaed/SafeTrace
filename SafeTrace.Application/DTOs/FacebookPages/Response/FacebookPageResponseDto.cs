namespace SafeTrace.Application.DTOs.FacebookPages.Response
{
    public class FacebookPageResponseDto
    {
        public long Id { get; set; }
        public string FacebookPageId { get; set; } = null!;
        public string PageName { get; set; } = null!;
        public string? PageUrl { get; set; }
        public string UserId { get; set; } = null!;
        public FacebookIntegrationStatus IntegrationStatus { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? TokenExpiresAt { get; set; }
        public DateTimeOffset? LastSyncedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
