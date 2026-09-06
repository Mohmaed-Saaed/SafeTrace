using System.Net;
using System.Net.Sockets;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.DTOs.Files.Request;

namespace SafeTrace.Infrastructure.Services
{
    public sealed class ExternalImageDownloadService : IExternalImageDownloadService
    {
        private const int MaxRedirects = 3;
        private const int MaxImageBytes = 5 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg",
                ["image/jpg"] = ".jpg",
                ["image/png"] = ".png",
                ["image/webp"] = ".webp"
            };

        private readonly HttpClient _httpClient;

        public ExternalImageDownloadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DownloadedImageDto> DownloadAsync(
            string imageUrl,
            CancellationToken cancellationToken = default)
        {
            var currentUri = await ValidateUriAsync(imageUrl, cancellationToken);

            for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.UserAgent.ParseAdd("SafeTrace/1.0");

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (IsRedirect(response.StatusCode))
                {
                    if (redirectCount == MaxRedirects ||
                        response.Headers.Location is null)
                    {
                        throw new BadRequestException(
                            "The Facebook image could not be downloaded safely.");
                    }

                    var redirectUri = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(currentUri, response.Headers.Location);

                    currentUri = await ValidateUriAsync(
                        redirectUri.AbsoluteUri,
                        cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new BadRequestException(
                        "A Facebook image is unavailable or has expired.");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (string.IsNullOrWhiteSpace(contentType) ||
                    !AllowedContentTypes.TryGetValue(contentType, out var extension))
                {
                    throw new BadRequestException(
                        "A Facebook attachment is not a supported image.");
                }

                if (response.Content.Headers.ContentLength > MaxImageBytes)
                {
                    throw new BadRequestException(
                        "A Facebook image exceeds the 5 MB limit.");
                }

                var content = await ReadWithLimitAsync(
                    response.Content,
                    cancellationToken);

                return new DownloadedImageDto(content, contentType, extension);
            }

            throw new BadRequestException(
                "The Facebook image could not be downloaded safely.");
        }

        private static async Task<Uri> ValidateUriAsync(
            string imageUrl,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new BadRequestException(
                    "A Facebook image URL is invalid.");
            }

            IPAddress[] addresses;

            try
            {
                addresses = await Dns.GetHostAddressesAsync(
                    uri.DnsSafeHost,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is SocketException or ArgumentException)
            {
                throw new BadRequestException(
                    "A Facebook image host could not be resolved.");
            }

            if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
            {
                throw new BadRequestException(
                    "A Facebook image URL is not allowed.");
            }

            return uri;
        }

        private static bool IsPrivateAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();

            if (IPAddress.IsLoopback(address) ||
                address.Equals(IPAddress.Any) ||
                address.Equals(IPAddress.IPv6Any) ||
                address.Equals(IPAddress.None) ||
                address.Equals(IPAddress.IPv6None))
            {
                return true;
            }

            var bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return bytes[0] == 10 ||
                       bytes[0] == 127 ||
                       (bytes[0] == 169 && bytes[1] == 254) ||
                       (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168) ||
                       bytes[0] >= 224;
            }

            return address.IsIPv6LinkLocal ||
                   address.IsIPv6SiteLocal ||
                   address.IsIPv6Multicast ||
                   (bytes[0] & 0xfe) == 0xfc;
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode is HttpStatusCode.MovedPermanently or
                HttpStatusCode.Redirect or
                HttpStatusCode.RedirectMethod or
                HttpStatusCode.TemporaryRedirect or
                HttpStatusCode.PermanentRedirect;
        }

        private static async Task<byte[]> ReadWithLimitAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            await using var source = await content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new MemoryStream();
            var buffer = new byte[81920];
            var totalBytes = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                    break;

                totalBytes += bytesRead;
                if (totalBytes > MaxImageBytes)
                {
                    throw new BadRequestException(
                        "A Facebook image exceeds the 5 MB limit.");
                }

                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }

            if (totalBytes == 0)
            {
                throw new BadRequestException(
                    "A Facebook image is empty.");
            }

            return destination.ToArray();
        }
    }
}
