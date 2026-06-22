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
        Infant,     // 0  - 2
        Child,      // 3  - 12
        Teenager,   // 13 - 17
        YoungAdult, // 18 - 35
        Adult,      // 36 - 59
        Senior      // 60 - 120
    }
}
