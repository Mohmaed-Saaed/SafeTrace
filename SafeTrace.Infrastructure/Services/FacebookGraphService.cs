using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Infrastructure.DTOs.FacebookGraph.Response;
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

        public Task<FacebookPageConnectionResultDto> ConnectPageAsync(string facebookPageId)
        {
            return ConnectPageCoreAsync(facebookPageId);
        }

        public Task<FacebookPageConnectionResultDto> ReconnectPageAsync(string facebookPageId)
        {
            return ConnectPageCoreAsync(facebookPageId);
        }

        public async Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            FacebookPage page,
            DateTimeOffset? since)
        {
            ArgumentNullException.ThrowIfNull(page);

            if (string.IsNullOrWhiteSpace(page.PageAccessToken))
            {
                throw new FacebookAuthenticationException(
                    "لا يوجد رمز وصول صالح لصفحة Facebook. أعد ربط الصفحة.");
            }

            var apiVersion = ValidateAndNormalizeApiVersion();
            var pageSize = Math.Clamp(_options.PageSize, 1, 100);
            var initialSyncLimit = Math.Max(1, _options.InitialSyncPostLimit);
            var posts = new List<FacebookPostDto>();
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);
            string? after = null;

            while (true)
            {
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
                    HttpCompletionOption.ResponseHeadersRead);

                var responseContent = await response.Content.ReadAsStringAsync();

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

        private async Task<FacebookPageConnectionResultDto> ConnectPageCoreAsync(
            string facebookPageId)
        {
            var normalizedPageId = NormalizeFacebookPageId(facebookPageId);

            if (string.IsNullOrWhiteSpace(_options.SystemUserAccessToken))
            {
                throw new BadRequestException(
                    "لم يتم إعداد رمز وصول حساب النظام الخاص بـ Meta. أضف FacebookGraph:SystemUserAccessToken في الأسرار قبل ربط الصفحة.");
            }

            var apiVersion = ValidateAndNormalizeApiVersion();
            var requestUrl =
                $"{apiVersion}/{Uri.EscapeDataString(normalizedPageId)}?fields=id%2Caccess_token";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.SystemUserAccessToken);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                ThrowGraphException(response.StatusCode, error: null);

            var graphResponse = DeserializeConnectionResponse(responseContent);

            if (!response.IsSuccessStatusCode || graphResponse.Error is not null)
                ThrowGraphException(response.StatusCode, graphResponse.Error);

            if (!string.Equals(
                    graphResponse.Id,
                    normalizedPageId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(graphResponse.PageAccessToken))
            {
                throw new BadRequestException(
                    "تعذر الحصول على صلاحية الوصول لصفحة Facebook المحددة. تأكد أن حساب النظام لديه صلاحيات الصفحة المطلوبة في Meta Business Manager.");
            }

            return new FacebookPageConnectionResultDto
            {
                FacebookPageId = graphResponse.Id!,
                PageAccessToken = graphResponse.PageAccessToken!,
                TokenExpiresAt = null
            };
        }

        private string ValidateAndNormalizeApiVersion()
        {
            var apiVersion = _options.ApiVersion.Trim().Trim('/');

            if (!Regex.IsMatch(apiVersion, @"^v\d+\.\d+$", RegexOptions.CultureInvariant))
            {
                throw new InvalidOperationException(
                    "إصدار Facebook Graph API غير مُعد بشكل صحيح.");
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
                    "أعاد Facebook Graph API استجابة غير صالحة.",
                    ex);
            }
        }

        private static FacebookGraphPageConnectionResponse DeserializeConnectionResponse(
            string responseContent)
        {
            try
            {
                return JsonSerializer.Deserialize<FacebookGraphPageConnectionResponse>(
                           responseContent,
                           SerializerOptions)
                       ?? new FacebookGraphPageConnectionResponse();
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException(
                    "أعاد Facebook Graph API استجابة غير صالحة.",
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
                    "تفويض Facebook غير صالح أو انتهت صلاحيته أو لم يعد يملك الصلاحيات المطلوبة.");
            }

            throw new HttpRequestException(
                $"فشل طلب Facebook Graph API برمز الحالة {(int)statusCode}.");
        }

        private static string NormalizeFacebookPageId(string facebookPageId)
        {
            if (string.IsNullOrWhiteSpace(facebookPageId))
            {
                throw new BadRequestException("معرّف صفحة Facebook مطلوب.");
            }

            var normalizedPageId = facebookPageId.Trim();

            if (normalizedPageId.Length > 100)
            {
                throw new BadRequestException("معرّف صفحة Facebook غير صالح.");
            }

            return normalizedPageId;
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
