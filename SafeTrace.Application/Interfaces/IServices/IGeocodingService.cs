using SafeTrace.Application.DTOs.Geocoding.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IGeocodingService
    {
        Task<GeocodingResultDto?> GeocodeAsync(
            string? government,
            string? city,
            string? street,
            CancellationToken cancellationToken = default);
    }
}
