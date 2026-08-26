using System.Text.Json.Serialization;

namespace SafeTrace.Infrastructure.Models.FacebookGraph
{
    internal sealed class FacebookGraphPostsResponse
    {
        [JsonPropertyName("data")]
        public List<FacebookGraphPost> Data { get; set; } = [];

        [JsonPropertyName("paging")]
        public FacebookGraphPaging? Paging { get; set; }

        [JsonPropertyName("error")]
        public FacebookGraphError? Error { get; set; }
    }

    internal sealed class FacebookGraphPost
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("permalink_url")]
        public string? PermalinkUrl { get; set; }

        [JsonPropertyName("created_time")]
        public DateTimeOffset? CreatedTime { get; set; }

        [JsonPropertyName("attachments")]
        public FacebookGraphAttachmentConnection? Attachments { get; set; }
    }

    internal sealed class FacebookGraphAttachmentConnection
    {
        [JsonPropertyName("data")]
        public List<FacebookGraphAttachment> Data { get; set; } = [];
    }

    internal sealed class FacebookGraphAttachment
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("media")]
        public FacebookGraphMedia? Media { get; set; }

        [JsonPropertyName("target")]
        public FacebookGraphTarget? Target { get; set; }

        [JsonPropertyName("subattachments")]
        public FacebookGraphAttachmentConnection? Subattachments { get; set; }
    }

    internal sealed class FacebookGraphMedia
    {
        [JsonPropertyName("image")]
        public FacebookGraphImage? Image { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    internal sealed class FacebookGraphImage
    {
        [JsonPropertyName("src")]
        public string? Source { get; set; }
    }

    internal sealed class FacebookGraphTarget
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    internal sealed class FacebookGraphPaging
    {
        [JsonPropertyName("cursors")]
        public FacebookGraphCursors? Cursors { get; set; }
    }

    internal sealed class FacebookGraphCursors
    {
        [JsonPropertyName("after")]
        public string? After { get; set; }
    }

    internal sealed class FacebookGraphError
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("error_subcode")]
        public int? ErrorSubcode { get; set; }
    }
}
