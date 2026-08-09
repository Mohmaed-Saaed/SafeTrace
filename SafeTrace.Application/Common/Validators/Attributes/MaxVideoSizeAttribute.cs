using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class MaxVideoSizeAttribute : ValidationAttribute
    {
        private readonly int _maxSizeMb;

        public MaxVideoSizeAttribute(int maxSizeMb)
        {
            _maxSizeMb = maxSizeMb;
        }

        public override bool IsValid(object? value)
        {
            if (value is not IFormFile file)
                return true;

            var maxSizeBytes = _maxSizeMb * 1024L * 1024L;

            return file.Length <= maxSizeBytes;
        }

        public override string FormatErrorMessage(string name)
        {
            return ErrorMessage ??
                   $"{name} must not exceed {_maxSizeMb} MB.";
        }
    }
}
