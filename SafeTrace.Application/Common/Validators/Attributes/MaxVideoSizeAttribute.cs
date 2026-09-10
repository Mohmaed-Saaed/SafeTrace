using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class MaxVideoSizeAttribute : ValidationAttribute
    {
        private readonly int _maxSizeMb;

        public MaxVideoSizeAttribute(int maxSizeMb)
        {
            _maxSizeMb = maxSizeMb;
            ErrorMessage = $"Video size must not exceed {maxSizeMb} MB.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            var maxSizeBytes = _maxSizeMb * 1024L * 1024L;

            if (value is IFormFile file)
                return IsFileValid(file, maxSizeBytes);

            if (value is IEnumerable<IFormFile> files)
                return files.All(f => IsFileValid(f, maxSizeBytes));

            return false;
        }

        private static bool IsFileValid(IFormFile file, long maxSizeBytes)
        {
            return file.Length > 0 && file.Length <= maxSizeBytes;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessageString, name, _maxSizeMb);
        }
    }
}