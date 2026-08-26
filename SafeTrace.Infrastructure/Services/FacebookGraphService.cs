using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Models.Facebook;
using SafeTrace.Infrastructure.Models.FacebookGraph;
using SafeTrace.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace SafeTrace.Infrastructure.Services
{
    public class FacebookGraphService : IFacebookGraphService
    {
        private const string PostFields =
            "id,message,permalink_url,created_time," +
            "attachments{media_type,media,target,subattachments{media_type,media,target}}";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly FacebookGraphOptions _options;

        public FacebookGraphService(
            HttpClient httpClient,
            IOptions<FacebookGraphOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public Task<FacebookPageConnectionResult> ConnectPageAsync(string facebookPageId)
        {
            // TODO: Complete Meta OAuth and validate that the authorized account can access facebookPageId.
            throw CreateNotConfiguredException();
        }

        public Task<FacebookPageConnectionResult> ReconnectPageAsync(string facebookPageId)
        {
            // TODO: Refresh/re-authorize through Meta OAuth without accepting an access token from the API request.
            throw CreateNotConfiguredException();
        }

        public async Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            FacebookPage page,
            DateTimeOffset? since,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (string.IsNullOrWhiteSpace(page.PageAccessToken))
            {
                throw new FacebookAuthenticationException(
                    "The Facebook page does not have a valid connection token.");
            }

            var apiVersion = ValidateAndNormalizeApiVersion();
            var pageSize = Math.Clamp(_options.PageSize, 1, 100);
            var initialSyncLimit = Math.Max(1, _options.InitialSyncPostLimit);
            var posts = new List<FacebookPostDto>();
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);
            string? after = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var requestLimit = since.HasValue
                    ? pageSize
                    : Math.Min(pageSize, initialSyncLimit - posts.Count);

                if (requestLimit <= 0)
                    break;

                var requestUrl = BuildPostsRequestUrl(
                    apiVersion,
                    page.FacebookPageId,
                    requestLimit,
                    since,
                    after);

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", page.PageAccessToken);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    ThrowGraphException(response.StatusCode, error: null);

                var graphResponse = DeserializeResponse(responseContent);

                if (!response.IsSuccessStatusCode || graphResponse.Error is not null)
                {
                    ThrowGraphException(response.StatusCode, graphResponse.Error);
                }

                posts.AddRange(graphResponse.Data.Select(MapPost));

                if (!since.HasValue && posts.Count >= initialSyncLimit)
                    break;

                after = graphResponse.Paging?.Cursors?.After;

                if (string.IsNullOrWhiteSpace(after) || !seenCursors.Add(after))
                    break;
            }

            return since.HasValue
                ? posts
                : posts.Take(initialSyncLimit).ToList();
        }

        private static BadRequestException CreateNotConfiguredException()
        {
            return new BadRequestException(
                "Facebook integration is not configured. Configure Meta OAuth before connecting Facebook pages.");
        }

        private string ValidateAndNormalizeApiVersion()
        {
            var apiVersion = _options.ApiVersion.Trim().Trim('/');

            if (!Regex.IsMatch(apiVersion, @"^v\d+\.\d+$", RegexOptions.CultureInvariant))
            {
                throw new InvalidOperationException(
                    "Facebook Graph API version is not configured correctly.");
            }

            return apiVersion;
        }

        private static string BuildPostsRequestUrl(
            string apiVersion,
            string facebookPageId,
            int limit,
            DateTimeOffset? since,
            string? after)
        {
            var query = new List<string>
            {
                $"fields={Uri.EscapeDataString(PostFields)}",
                $"limit={limit}"
            };

            if (since.HasValue)
                query.Add($"since={since.Value.ToUnixTimeSeconds()}");

            if (!string.IsNullOrWhiteSpace(after))
                query.Add($"after={Uri.EscapeDataString(after)}");

            return $"{apiVersion}/{Uri.EscapeDataString(facebookPageId)}/posts?{string.Join('&', query)}";
        }

        private static FacebookGraphPostsResponse DeserializeResponse(string responseContent)
        {
            try
            {
                return JsonSerializer.Deserialize<FacebookGraphPostsResponse>(
                           responseContent,
                           SerializerOptions)
                       ?? new FacebookGraphPostsResponse();
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException(
                    "Facebook Graph API returned an invalid response.",
                    ex);
            }
        }

        private static void ThrowGraphException(
            HttpStatusCode statusCode,
            FacebookGraphError? error)
        {
            var isAuthenticationFailure =
                statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
                error?.Code is 10 or 102 or 190 or 200 ||
                string.Equals(error?.Type, "OAuthException", StringComparison.OrdinalIgnoreCase);

            if (isAuthenticationFailure)
            {
                throw new FacebookAuthenticationException(
                    "Facebook authorization is invalid, expired, or no longer has the required permissions.");
            }

            throw new HttpRequestException(
                $"Facebook Graph API request failed with status code {(int)statusCode}.");
        }

        private static FacebookPostDto MapPost(FacebookGraphPost post)
        {
            var media = (post.Attachments?.Data ?? [])
                .SelectMany(FlattenAttachments)
                .Select(attachment => new FacebookPostMediaDto
                {
                    FacebookMediaId = attachment.Target?.Id ?? attachment.Id,
                    FileUrl = attachment.Media?.Image?.Source
                              ?? attachment.Media?.Source
                              ?? string.Empty
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.FileUrl))
                .DistinctBy(item => item.FileUrl, StringComparer.Ordinal)
                .ToList();

            return new FacebookPostDto
            {
                FacebookPostId = post.Id,
                PostText = post.Message,
                PostUrl = post.PermalinkUrl,
                PublishedAt = post.CreatedTime,
                Media = media
            };
        }

        private static IEnumerable<FacebookGraphAttachment> FlattenAttachments(
            FacebookGraphAttachment attachment)
        {
            yield return attachment;

            foreach (var child in attachment.Subattachments?.Data ?? [])
            {
                foreach (var descendant in FlattenAttachments(child))
                    yield return descendant;
            }
        }
    }
}
