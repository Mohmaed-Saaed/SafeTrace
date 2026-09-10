namespace SafeTrace.Infrastructure.Options
{
    public sealed class GeocodingOptions
    {
        public const string SectionName = "Geocoding";
        public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/geocode/";
        public string ApiKey { get; set; } = string.Empty;
    }
}
