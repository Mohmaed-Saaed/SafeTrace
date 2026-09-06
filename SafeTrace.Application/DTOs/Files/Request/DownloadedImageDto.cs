using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.Files.Request
{
    /// <summary>
    /// ملف صورة داخلي تم تنزيله من مصدر موثوق قبل رفعه إلى التخزين.
    /// </summary>
    public sealed class DownloadedImageDto : IDisposable
    {
        private readonly MemoryStream _content;

        public DownloadedImageDto(byte[] content, string contentType, string extension)
        {
            _content = new MemoryStream(content, writable: false);
            var fileName = $"facebook-{Guid.NewGuid():N}{extension}";
            File = new FormFile(_content, 0, _content.Length, "file", fileName)
            {
                ContentType = contentType
            };
        }

        public IFormFile File { get; }

        public void Dispose()
        {
            _content.Dispose();
        }
    }
}
