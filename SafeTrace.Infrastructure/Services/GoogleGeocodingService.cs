using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SafeTrace.Application.DTOs.Geocoding.Response;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Enums;
using SafeTrace.Infrastructure.Options;

namespace SafeTrace.Infrastructure.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private const string EgyptCountryCode = "EG";

        private readonly HttpClient _httpClient;
        private readonly GeocodingOptions _options;

        public GoogleGeocodingService(
            HttpClient httpClient,
            IOptions<GeocodingOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<GeocodingResultDto?> GeocodeAsync(
            string? government,
            string? city,
            string? street)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "مفتاح Google Geocoding API غير مهيأ.");
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
                response = await _httpClient.GetAsync(requestUrl);
            }
            catch (Exception)
            {
                throw new HttpRequestException(
                    "تعذر إكمال طلب تحديد الموقع الجغرافي من Google.");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"فشل طلب تحديد الموقع الجغرافي من Google برمز الحالة {(int)response.StatusCode}.");
                }

                await using var responseStream =
                    await response.Content.ReadAsStreamAsync();

                using var doc = await JsonDocument.ParseAsync(responseStream);
                var root = doc.RootElement;

                var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

                if (string.Equals(status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException(
                        "أرجع Google Geocoding استجابة غير ناجحة.");
                }

                if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                foreach (var result in results.EnumerateArray())
                {
                    if (!IsEgyptResult(result))
                        continue;

                    if (!result.TryGetProperty("geometry", out var geometry) ||
                        !geometry.TryGetProperty("location", out var location))
                    {
                        continue;
                    }

                    if (!location.TryGetProperty("lat", out var latProp) || !latProp.TryGetDouble(out var lat) ||
                        !location.TryGetProperty("lng", out var lngProp) || !lngProp.TryGetDouble(out var lng))
                    {
                        continue;
                    }

                    var gov = GetAddressComponent(result, "administrative_area_level_1");

                    return new GeocodingResultDto
                    {
                        Latitude = lat,
                        Longitude = lng,
                        LocationAccuracy = query.Value.Accuracy,
                        Government = gov
                    };
                }

                return null;
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

        private static bool IsEgyptResult(JsonElement result)
        {
            if (!result.TryGetProperty("address_components", out var components) ||
                components.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var component in components.EnumerateArray())
            {
                if (ComponentHasType(component, "country"))
                {
                    var shortName = component.TryGetProperty("short_name", out var sn) ? sn.GetString() : null;
                    return string.Equals(shortName, EgyptCountryCode, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static string? GetAddressComponent(JsonElement result, string type)
        {
            if (!result.TryGetProperty("address_components", out var components) ||
                components.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var component in components.EnumerateArray())
            {
                if (ComponentHasType(component, type))
                {
                    return component.TryGetProperty("long_name", out var ln) ? ln.GetString() : null;
                }
            }

            return null;
        }

        private static bool ComponentHasType(JsonElement component, string type)
        {
            if (component.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in types.EnumerateArray())
                {
                    if (string.Equals(t.GetString(), type, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
