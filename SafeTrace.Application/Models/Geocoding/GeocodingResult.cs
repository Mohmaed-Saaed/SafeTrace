namespace SafeTrace.Application.Models.Geocoding
{
    public sealed class GeocodingResult
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public LocationAccuracy LocationAccuracy { get; init; }
        public string? Government { get; init; }
    }
}
