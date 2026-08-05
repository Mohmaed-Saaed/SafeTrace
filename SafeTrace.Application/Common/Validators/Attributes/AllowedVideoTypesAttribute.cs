using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class AllowedVideoTypesAttribute :ValidationAttribute
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".mov",
            ".webm"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4",
            "video/quicktime",
            "video/webm"
        };

        public AllowedVideoTypesAttribute()
            : base("Only MP4, MOV, and WebM videos are allowed.")
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

            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return false;

            if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
                return false;

            return true;
        }
    }
}
