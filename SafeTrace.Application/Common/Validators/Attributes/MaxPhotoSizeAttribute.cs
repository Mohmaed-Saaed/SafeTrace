using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Ensures every uploaded file is within the size limit (in MB).</summary>
    public class MaxPhotoSizeAttribute : ValidationAttribute
    {
        private readonly long _maxBytes;
        public MaxPhotoSizeAttribute(int maxMb) => _maxBytes = maxMb * 1024L * 1024L;

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is IFormFile file)
                return file.Length <= _maxBytes;
            if (value is List<IFormFile> files)
                return files.All(f => f.Length <= _maxBytes);
            return false;
        }
    }
}
