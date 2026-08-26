using System.Text.Json;
using Microsoft.Extensions.Options;
using SafeTrace.Application.Models.Geocoding;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Models.Geocoding;
using SafeTrace.Infrastructure.Options;

namespace SafeTrace.Infrastructure.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private const string EgyptCountryCode = "EG";
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly GeocodingOptions _options;

        public GoogleGeocodingService(
            HttpClient httpClient,
            IOptions<GeocodingOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<GeocodingResult?> GeocodeAsync(
            string? government,
            string? city,
            string? street,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Google Geocoding API key is not configured.");
            }

            var query = BuildEgyptQuery(government, city, street);

            if (query is null)
                return null;

            var requestUrl =
                $"json?address={Uri.EscapeDataString(query.Value.Address)}" +
                $"&components={Uri.EscapeDataString("country:EG")}" +
                "&region=eg&language=ar" +
                $"&key={Uri.EscapeDataString(_options.ApiKey)}";

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                throw new HttpRequestException(
                    "Google Geocoding request could not be completed.");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Google Geocoding request failed with status code {(int)response.StatusCode}.");
                }

                await using var responseStream =
                    await response.Content.ReadAsStreamAsync(cancellationToken);

                var payload = await JsonSerializer.DeserializeAsync<GoogleGeocodingResponse>(
                    responseStream,
                    SerializerOptions,
                    cancellationToken);

                if (payload is null ||
                    string.Equals(payload.Status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (!string.Equals(payload.Status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException(
                        "Google Geocoding returned an unsuccessful response.");
                }

                var egyptResult = payload.Results.FirstOrDefault(result =>
                    IsEgyptResult(result) &&
                    result.Geometry.Location?.Latitude is not null &&
                    result.Geometry.Location.Longitude is not null);

                if (egyptResult is null)
                    return null;

                return new GeocodingResult
                {
                    Latitude = egyptResult.Geometry.Location!.Latitude!.Value,
                    Longitude = egyptResult.Geometry.Location.Longitude!.Value,
                    LocationAccuracy = query.Value.Accuracy,
                    Government = GetAddressComponent(
                        egyptResult,
                        "administrative_area_level_1")?.LongName
                };
            }
        }

        private static (string Address, LocationAccuracy Accuracy)? BuildEgyptQuery(
            string? government,
            string? city,
            string? street)
        {
            government = NormalizeOptional(government);
            city = NormalizeOptional(city);
            street = NormalizeOptional(street);

            if (city is not null && government is null)
                return ($"{city}, Egypt", LocationAccuracy.City);

            if (street is not null && city is not null && government is not null)
            {
                return ($"{street}, {city}, {government}, Egypt", LocationAccuracy.Street);
            }

            if (city is not null && government is not null)
                return ($"{city}, {government}, Egypt", LocationAccuracy.City);

            if (government is not null)
                return ($"{government}, Egypt", LocationAccuracy.Governorate);

            return null;
        }

        private static bool IsEgyptResult(GoogleGeocodingResult result)
        {
            var country = GetAddressComponent(result, "country");

            return string.Equals(
                country?.ShortName,
                EgyptCountryCode,
                StringComparison.OrdinalIgnoreCase);
        }

        private static GoogleAddressComponent? GetAddressComponent(
            GoogleGeocodingResult result,
            string type)
        {
            return result.AddressComponents.FirstOrDefault(component =>
                component.Types.Contains(type, StringComparer.OrdinalIgnoreCase));
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
