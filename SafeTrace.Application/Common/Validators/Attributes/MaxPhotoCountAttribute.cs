using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Limits the number of uploaded files.</summary>
    public class MaxPhotoCountAttribute : ValidationAttribute
    {
        private readonly int _max;
        public MaxPhotoCountAttribute(int max) => _max = max;

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is List<IFormFile> files)
                return files.Count <= _max;
            return false;
        }
    }
}
