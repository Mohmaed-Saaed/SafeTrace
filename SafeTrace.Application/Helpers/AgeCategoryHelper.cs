using SafeTrace.Application.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.Common.Helpers
{
    /// <summary>
    /// Converts between a raw <c>Age</c> value and an <see cref="AgeCategory"/>.
    /// If the team decides on different ranges, this is the only place to edit.
    /// </summary>
    public static class AgeCategoryHelper
    {
        public static AgeCategory GetCategory(int age)
        {
            if (age <= 12) return AgeCategory.Child;
            if (age <= 17) return AgeCategory.Teenager;
            if (age <= 59) return AgeCategory.Adult;
            return AgeCategory.Elderly;
        }

        public static (int Min, int Max) GetRange(AgeCategory category) => category switch
        {
            AgeCategory.Child => (0, 12),
            AgeCategory.Teenager => (13, 17),
            AgeCategory.Adult => (18, 59),
            AgeCategory.Elderly => (60, 150),
            _ => (0, 150)
        };
    }
}
