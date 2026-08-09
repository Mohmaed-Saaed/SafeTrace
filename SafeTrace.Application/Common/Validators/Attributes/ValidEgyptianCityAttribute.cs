using System.ComponentModel.DataAnnotations;
using SafeTrace.Application.Constants;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class ValidEgyptianCityAttribute : ValidationAttribute
    {
        private readonly string _governoratePropertyName;

        public ValidEgyptianCityAttribute(string governoratePropertyName)
        {
            _governoratePropertyName = governoratePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var city = value as string;

            // Let [Required] handle null or empty validation
            if (string.IsNullOrWhiteSpace(city))
                return ValidationResult.Success;

            // Get the value of the governorate property
            var governorateProperty = validationContext.ObjectType.GetProperty(_governoratePropertyName);
            if (governorateProperty == null)
            {
                return new ValidationResult($"Property '{_governoratePropertyName}' not found on {validationContext.ObjectType.Name}");
            }

            var governorateValue = governorateProperty.GetValue(validationContext.ObjectInstance, null) as string;

            // If the governorate isn't provided or valid, we don't validate city yet (or we can assume city is invalid).
            // Usually we'd want a separate validation for governorate to ensure it is valid first.
            if (string.IsNullOrWhiteSpace(governorateValue))
            {
                return ValidationResult.Success;
            }

            if (!EgyptLocations.Governorates.TryGetValue(governorateValue.Trim(), out var cities))
            {
                // This means the governorate is invalid. 
                // There will be a separate attribute validating the governorate, so let this pass for now or fail city.
                // It's safer to fail if governorate is provided but invalid, but since we are writing a city validator:
                return new ValidationResult("Invalid Governorate.");
            }

            // Check if city exists in the governorate's cities list
            if (!cities.Any(c => string.Equals(c, city.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult("Invalid City for the selected Governorate.");
            }

            return ValidationResult.Success;
        }
    }

    public class ValidEgyptianGovernorateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            var governorate = value as string;

            // Let [Required] handle null or empty validation
            if (string.IsNullOrWhiteSpace(governorate))
                return true;

            return EgyptLocations.Governorates.ContainsKey(governorate.Trim());
        }
    }
}
