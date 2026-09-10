using System.Text.RegularExpressions;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>
    /// Validates that the text contains only Arabic letters and spaces.
    /// </summary>
    public class ArabicTextAttribute : ValidationAttribute
    {
        private static readonly Regex ArabicRegex = new(
            @"^[\u0621-\u064A]+(?:\s+[\u0621-\u064A]+)*$",
            RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true; // Let [Required] handle nulls

            var text = value.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return true;

            return ArabicRegex.IsMatch(text);
        }
    }
}