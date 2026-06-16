namespace SafeTrace.Application.Helpers
{
    /// <summary>
    /// Helper class for geographic calculations.
    /// Uses the Haversine formula to calculate distance between two coordinates.
    /// </summary>
    public static class GeoHelper
    {
        // Average Earth radius in kilometers according to WGS84 standard
        private const double EarthRadiusKm = 6371.0088;

        // Conversion factor from kilometers to miles
        private const double KmToMiles = 0.621371;

        /// <summary>
        /// Calculates the distance between two coordinates in kilometers.
        /// </summary>
        public static double DistanceKm(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            // Validate coordinates before calculation
            Validate(lat1, lon1);
            Validate(lat2, lon2);

            // Fast path:
            // If both points are identical, distance is zero
            if (lat1 == lat2 && lon1 == lon2)
                return 0;

            // Convert latitude values from degrees to radians
            double lat1Rad = ToRad(lat1);
            double lat2Rad = ToRad(lat2);

            // Calculate latitude difference in radians
            double dLat = ToRad(lat2 - lat1);

            // Calculate longitude difference in radians
            double dLon = ToRad(lon2 - lon1);

            // Apply Haversine formula
            double a = HaversineCore(
                lat1Rad,
                lat2Rad,
                dLat,
                dLon);

            // Protect against floating-point precision issues
            // a must always be between 0 and 1
            a = Clamp(a, 0.0, 1.0);

            // Calculate the angular distance in radians
            double c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            // Convert angular distance to kilometers
            return EarthRadiusKm * c;
        }

        /// <summary>
        /// Calculates the distance between two coordinates in miles.
        /// </summary>
        public static double DistanceMiles(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            // Reuse kilometer calculation then convert to miles
            return DistanceKm(lat1, lon1, lat2, lon2) * KmToMiles;
        }

        /// <summary>
        /// Core Haversine formula.
        /// Returns the intermediate value "a".
        /// </summary>
        private static double HaversineCore(
            double lat1Rad,
            double lat2Rad,
            double dLat,
            double dLon)
        {
            // sin²(Δlat / 2)
            double sinDLat = Math.Sin(dLat / 2);

            // sin²(Δlon / 2)
            double sinDLon = Math.Sin(dLon / 2);

            // Haversine equation:
            // a = sin²(Δlat/2)
            //   + cos(lat1) * cos(lat2) * sin²(Δlon/2)
            return sinDLat * sinDLat +
                   Math.Cos(lat1Rad) *
                   Math.Cos(lat2Rad) *
                   sinDLon * sinDLon;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// Math trig functions require radians.
        /// </summary>
        private static double ToRad(double angle)
            => angle * (Math.PI / 180.0);

        /// <summary>
        /// Validates latitude and longitude ranges.
        /// </summary>
        private static void Validate(double lat, double lon)
        {
            // Check for invalid numeric values
            if (double.IsNaN(lat) || double.IsNaN(lon))
                throw new ArgumentException(
                    "Coordinates cannot be NaN.");

            // Check for infinity values
            if (double.IsInfinity(lat) || double.IsInfinity(lon))
                throw new ArgumentException(
                    "Coordinates cannot be infinite.");

            // Latitude range:
            // South Pole = -90
            // North Pole = 90
            if (lat < -90 || lat > 90)
                throw new ArgumentOutOfRangeException(
                    nameof(lat),
                    "Latitude must be between -90 and 90.");

            // Longitude range:
            // West = -180
            // East = 180
            if (lon < -180 || lon > 180)
                throw new ArgumentOutOfRangeException(
                    nameof(lon),
                    "Longitude must be between -180 and 180.");
        }

        /// <summary>
        /// Restricts a value to a specified range.
        /// Used to avoid floating-point precision errors.
        /// </summary>
        private static double Clamp(
            double value,
            double min,
            double max)
        {
            return value < min
                ? min
                : (value > max ? max : value);
        }
    }
}