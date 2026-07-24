using System.Text.RegularExpressions;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class EgyptianPhoneAttribute : ValidationAttribute
    {
        private static readonly Regex PhoneRegex = new(
            @"^(?:\+20|0020|0)?1[0125]\d{8}$",
            RegexOptions.Compiled);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var phone = value.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(phone))
                return ValidationResult.Success;

            return PhoneRegex.IsMatch(phone)
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? "Please enter a valid Egyptian mobile number.");
        }
    }
}

