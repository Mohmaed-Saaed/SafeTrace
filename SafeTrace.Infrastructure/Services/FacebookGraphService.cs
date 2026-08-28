using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.FacebookPosts.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Options;

namespace SafeTrace.Infrastructure.Services
{
    public class FacebookGraphService : IFacebookGraphService
    {
        private const string PostFields =
            "id,message,permalink_url,created_time," +
            "attachments{media_type,media,target,subattachments{media_type,media,target}}";

        private readonly HttpClient _httpClient;
        private readonly FacebookGraphOptions _options;

        public FacebookGraphService(
            HttpClient httpClient,
            IOptions<FacebookGraphOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<FacebookPageConnectionResultDto> ConnectPageAsync(string facebookPageId, string pageAccessToken)
        {
            var normalizedPageId = NormalizeFacebookPageId(facebookPageId);

            if (string.IsNullOrWhiteSpace(pageAccessToken))
            {
                throw new FacebookAuthenticationException(
                    "لا يوجد رمز وصول صالح لصفحة Facebook.");
            }

            var apiVersion = ValidateAndNormalizeApiVersion();
            var requestUrl = BuildPageRequestUrl(apiVersion, normalizedPageId);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pageAccessToken);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var responseContent = await response.Content.ReadAsStringAsync();

            using var doc = ParseJson(responseContent);
            var root = doc.RootElement;

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
                !response.IsSuccessStatusCode ||
                root.TryGetProperty("error", out _))
            {
                ThrowGraphException(response.StatusCode, root);
            }

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            if (!string.Equals(id, normalizedPageId, StringComparison.Ordinal))
            {
                throw new BadRequestException(
                    "تعذر التحقق من صفحة Facebook المحددة. تأكد من صحة معرّف الصفحة ورمز الوصول.");
            }

            return new FacebookPageConnectionResultDto
            {
                FacebookPageId = id!,
                PageAccessToken = pageAccessToken,
                TokenExpiresAt = null
            };
        }

        public async Task<IReadOnlyList<FacebookPostDto>> GetNewPostsAsync(
            string facebookPageId,
            string pageAccessToken,
            DateTimeOffset? since)
        {
            var normalizedPageId = NormalizeFacebookPageId(facebookPageId);

            if (string.IsNullOrWhiteSpace(pageAccessToken))
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
                    normalizedPageId,
                    requestLimit,
                    since,
                    after);

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", pageAccessToken);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead);

                var responseContent = await response.Content.ReadAsStringAsync();

                using var doc = ParseJson(responseContent);
                var root = doc.RootElement;

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
                    !response.IsSuccessStatusCode ||
                    root.TryGetProperty("error", out _))
                {
                    ThrowGraphException(response.StatusCode, root);
                }

                if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var postElement in dataArray.EnumerateArray())
                    {
                        posts.Add(MapPost(postElement));
                    }
                }

                if (!since.HasValue && posts.Count >= initialSyncLimit)
                    break;

                if (root.TryGetProperty("paging", out var paging) &&
                    paging.TryGetProperty("cursors", out var cursors) &&
                    cursors.TryGetProperty("after", out var afterProp))
                {
                    after = afterProp.GetString();
                }
                else
                {
                    after = null;
                }

                if (string.IsNullOrWhiteSpace(after) || !seenCursors.Add(after))
                    break;
            }

            return since.HasValue
                ? posts
                : posts.Take(initialSyncLimit).ToList();
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

        private static string BuildPageRequestUrl(string apiVersion, string facebookPageId)
        {
            return $"{apiVersion}/{Uri.EscapeDataString(facebookPageId)}?fields=id%2Cname";
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

        private static JsonDocument ParseJson(string responseContent)
        {
            try
            {
                return JsonDocument.Parse(responseContent);
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException(
                    "أعاد Facebook Graph API استجابة غير صالحة.",
                    ex);
            }
        }

        private static void ThrowGraphException(HttpStatusCode statusCode, JsonElement root)
        {
            int? code = null;
            string? type = null;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("code", out var codeProp) && codeProp.TryGetInt32(out var c))
                    code = c;
                if (error.TryGetProperty("type", out var typeProp))
                    type = typeProp.GetString();
            }

            var isAuthenticationFailure =
                statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
                code is 10 or 102 or 190 or 200 ||
                string.Equals(type, "OAuthException", StringComparison.OrdinalIgnoreCase);

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

        private static FacebookPostDto MapPost(JsonElement post)
        {
            var postId = post.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
            var message = post.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
            var permalinkUrl = post.TryGetProperty("permalink_url", out var urlProp) ? urlProp.GetString() : null;

            DateTimeOffset? createdTime = null;
            if (post.TryGetProperty("created_time", out var timeProp) &&
                timeProp.TryGetDateTimeOffset(out var parsedTime))
            {
                createdTime = parsedTime;
            }

            var mediaList = new List<FacebookPostMediaDto>();

            if (post.TryGetProperty("attachments", out var attachments) &&
                attachments.TryGetProperty("data", out var attachData) &&
                attachData.ValueKind == JsonValueKind.Array)
            {
                ExtractMedia(attachData, mediaList);
            }

            var distinctMedia = mediaList
                .Where(item => !string.IsNullOrWhiteSpace(item.FileUrl))
                .DistinctBy(item => item.FileUrl, StringComparer.Ordinal)
                .ToList();

            return new FacebookPostDto
            {
                FacebookPostId = postId,
                PostText = message,
                PostUrl = permalinkUrl,
                PublishedAt = createdTime,
                Media = distinctMedia
            };
        }

        private static void ExtractMedia(JsonElement attachmentsArray, List<FacebookPostMediaDto> mediaList)
        {
            foreach (var attachment in attachmentsArray.EnumerateArray())
            {
                string? mediaId = null;
                if (attachment.TryGetProperty("target", out var target) &&
                    target.TryGetProperty("id", out var targetId))
                {
                    mediaId = targetId.GetString();
                }
                else if (attachment.TryGetProperty("id", out var attachId))
                {
                    mediaId = attachId.GetString();
                }

                string? fileUrl = null;
                if (attachment.TryGetProperty("media", out var media))
                {
                    if (media.TryGetProperty("image", out var image) &&
                        image.TryGetProperty("src", out var src))
                    {
                        fileUrl = src.GetString();
                    }
                    else if (media.TryGetProperty("source", out var source))
                    {
                        fileUrl = source.GetString();
                    }
                }

                if (!string.IsNullOrWhiteSpace(fileUrl))
                {
                    mediaList.Add(new FacebookPostMediaDto
                    {
                        FacebookMediaId = mediaId,
                        FileUrl = fileUrl
                    });
                }

                if (attachment.TryGetProperty("subattachments", out var subattachments) &&
                    subattachments.TryGetProperty("data", out var subData) &&
                    subData.ValueKind == JsonValueKind.Array)
                {
                    ExtractMedia(subData, mediaList);
                }
            }
        }
    }
}
