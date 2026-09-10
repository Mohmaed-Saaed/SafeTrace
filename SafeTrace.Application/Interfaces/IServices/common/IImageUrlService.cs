using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Interfaces.IServices.common
{
    public interface IImageUrlService
    {
        string? Build(string? path);
    }
}
