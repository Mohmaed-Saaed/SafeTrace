
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Ensures every uploaded file is JPEG, PNG, or WebP.</summary>
    public class AllowedPhotoTypesAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> _allowed =
            ["image/jpeg", "image/png", "image/webp"];

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is List<IFormFile> files)
                return files.All(f => _allowed.Contains(f.ContentType.ToLowerInvariant()));
            return false;
        }
    }
}
