using System.Text.Json.Serialization;

namespace SafeTrace.Infrastructure.DTOs.Geocoding.Response
{
    internal sealed class GoogleGeocodingResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("results")]
        public List<GoogleGeocodingResult> Results { get; set; } = [];
    }

    internal sealed class GoogleGeocodingResult
    {
        [JsonPropertyName("address_components")]
        public List<GoogleAddressComponent> AddressComponents { get; set; } = [];

        [JsonPropertyName("geometry")]
        public GoogleGeometry Geometry { get; set; } = new();
    }

    internal sealed class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string? LongName { get; set; }

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = [];
    }

    internal sealed class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleCoordinates? Location { get; set; }
    }

    internal sealed class GoogleCoordinates
    {
        [JsonPropertyName("lat")]
        public double? Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double? Longitude { get; set; }
    }
}
