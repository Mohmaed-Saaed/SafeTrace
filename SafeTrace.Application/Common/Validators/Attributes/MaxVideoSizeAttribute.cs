using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class MaxVideoSizeAttribute : ValidationAttribute
    {
        private readonly long _maxBytes;

        public MaxVideoSizeAttribute(int maxMb)
            => _maxBytes = maxMb * 1024L * 1024L;

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            if (value is IFormFile file)
                return file.Length <= _maxBytes;

            if (value is List<IFormFile> files)
                return files.All(f => f.Length <= _maxBytes);

            return false;
        }
    }
}
