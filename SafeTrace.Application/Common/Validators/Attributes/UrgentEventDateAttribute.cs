namespace SafeTrace.Application.Common.Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UrgentEventDateAttribute : ValidationAttribute
    {
        private readonly double _maxHoursAgo;

        public UrgentEventDateAttribute(double maxHoursAgo = 24)
        {
            _maxHoursAgo = maxHoursAgo;
        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (value is not DateTime dt)
            {
                return new ValidationResult("Invalid event date format.");
            }

            DateTime utcEventDate;

            try
            {
                if (dt.Kind == DateTimeKind.Utc)
                {
                    utcEventDate = dt;
                }
                else
                {
                    // datetime-local from browser has no timezone information.
                    // Treat it as Egypt local time then convert to UTC.

                    var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                        "Africa/Cairo"
                    );

                    var egyptLocalDate = DateTime.SpecifyKind(
                        dt,
                        DateTimeKind.Unspecified
                    );

                    utcEventDate = TimeZoneInfo.ConvertTimeToUtc(
                        egyptLocalDate,
                        egyptTimeZone
                    );
                }
            }
            catch
            {
                // Fallback for environments where timezone id is different
                utcEventDate = DateTime.SpecifyKind(
                    dt,
                    DateTimeKind.Local
                ).ToUniversalTime();
            }


            DateTime now = DateTime.UtcNow;


            // Future date validation
            if (utcEventDate > now.AddMinutes(1))
            {
                return new ValidationResult(
                    "Event date cannot be in the future."
                );
            }


            // Maximum age validation
            if (utcEventDate < now.AddHours(-_maxHoursAgo))
            {
                return new ValidationResult(
                    $"Event date must be within the last {_maxHoursAgo} hours."
                );
            }


            return ValidationResult.Success;
        }
    }
}