using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class AllowedMessageFileTypesAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",

            // Videos
            ".mp4",
            ".mov",
            ".webm"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            "image/jpeg",
            "image/png",
            "image/webp",

            // Videos
            "video/mp4",
            "video/quicktime",
            "video/webm"
        };

        public AllowedMessageFileTypesAttribute()
            : base("Only JPG, JPEG, PNG, WebP, MP4, MOV, and WebM files are allowed.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

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

            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return false;

            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !AllowedContentTypes.Contains(file.ContentType))
                return false;

            return true;
        }
    }
}