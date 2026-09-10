using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class MaxMessageFileSizeAttribute : ValidationAttribute
    {
        private const int ImageMaxSizeMb = 5;
        private const int VideoMaxSizeMb = 50;

        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly string[] VideoExtensions =
        {
            ".mp4", ".mov", ".webm"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (ImageExtensions.Contains(extension))
            {
                if (file.Length > ImageMaxSizeMb * 1024 * 1024)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"Each photo must not exceed {ImageMaxSizeMb} MB.");
                }
            }
            else if (VideoExtensions.Contains(extension))
            {
                if (file.Length > VideoMaxSizeMb * 1024 * 1024)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"Each video must not exceed {VideoMaxSizeMb} MB.");
                }
            }

            return ValidationResult.Success;
        }
    }
}