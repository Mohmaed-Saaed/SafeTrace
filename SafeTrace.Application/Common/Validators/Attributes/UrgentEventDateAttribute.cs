using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UrgentEventDateAttribute : ValidationAttribute
    {
        private readonly double _maxHoursAgo;

        public UrgentEventDateAttribute(double maxHoursAgo = 6)
        {
            _maxHoursAgo = maxHoursAgo;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTime dt)
            {
                DateTime utcEventDate = dt.Kind == DateTimeKind.Utc
                    ? dt
                    : (dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc));

                DateTime now = DateTime.UtcNow;

                if (utcEventDate > now.AddMinutes(1))
                {
                    return new ValidationResult("Event date cannot be in the future.");
                }

                if (utcEventDate < now.AddHours(-_maxHoursAgo))
                {
                    return new ValidationResult($"Event date must be within the last {_maxHoursAgo} hours.");
                }

                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid event date format.");
        }
    }
}
