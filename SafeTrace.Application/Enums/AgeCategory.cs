using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.Common.Enums
{
    /// <summary>
    /// Age categories used for filtering Long-Term / Unknown person cases (FR-24, FR-29).
    /// Ranges are defined in <see cref="SafeTrace.Application.Common.Helpers.AgeCategoryHelper"/>.
    /// </summary>
    public enum AgeCategory
    {
        Child,    // 0  - 12
        Teenager, // 13 - 17
        Adult,    // 18 - 59
        Elderly   // 60+
    }
}
