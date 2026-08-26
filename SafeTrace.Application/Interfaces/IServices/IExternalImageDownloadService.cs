using SafeTrace.Application.DTOs.Files.Request;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IExternalImageDownloadService
    {
        Task<DownloadedImageDto> DownloadAsync(
            string imageUrl,
            CancellationToken cancellationToken = default);
    }
}
