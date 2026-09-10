using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Ensures every uploaded file is a valid image type by validating both file extension and MIME content-type.</summary>
    public class AllowedPhotoTypesAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/jpg"
        };

        public AllowedPhotoTypesAttribute() 
            : base("Only JPG, JPEG, PNG, and WebP images are allowed.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;

            if (value is IFormFile file)
                return IsFileValid(file);

            if (value is IEnumerable<IFormFile> files)
                return files.All(IsFileValid);

            return false;
        }

        private static bool IsFileValid(IFormFile file)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.FileName))
                return false;

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return false;

            if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
                return false;

            return true;
        }
    }
}
