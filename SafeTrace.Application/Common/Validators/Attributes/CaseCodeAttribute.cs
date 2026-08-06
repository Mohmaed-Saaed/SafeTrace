using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>
    /// Validates that the text matches the Case Code format (e.g. URG-123).
    /// </summary>
    public class CaseCodeAttribute : ValidationAttribute
    {
        private static readonly Regex CaseCodeRegex = new(
            @"^(URG|LNG|UNK)-\d+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const int MaxLength = 20;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success; // Let [Required] handle nulls

            var text = value.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return ValidationResult.Success;

            if (text.Length > MaxLength)
                return new ValidationResult($"كود الحالة يجب ألا يتجاوز {MaxLength} حرفاً.");

            if (!CaseCodeRegex.IsMatch(text))
                return new ValidationResult("أدخل كود حالة صحيح (مثال: URG-123).");

            return ValidationResult.Success;
        }
    }
}
