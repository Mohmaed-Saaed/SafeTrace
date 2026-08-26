using SafeTrace.Application.Models.Geocoding;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IGeocodingService
    {
        Task<GeocodingResult?> GeocodeAsync(
            string? government,
            string? city,
            string? street,
            CancellationToken cancellationToken = default);
    }
}
