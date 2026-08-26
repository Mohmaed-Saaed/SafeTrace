namespace SafeTrace.Application.DTOs.Geocoding.Response
{
    public sealed class GeocodingResultDto
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public LocationAccuracy LocationAccuracy { get; init; }
        public string? Government { get; init; }
    }
}
