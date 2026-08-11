using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Interfaces.IServices.common;

namespace SafeTrace.Application.Services
{
    public class ImageUrlService : IImageUrlService
    {
        private readonly IHttpContextAccessor _http;

        public ImageUrlService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public string? Build(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return $"{SystemConstants.filesBaseUrl}/{path}";
        }
    }
}
