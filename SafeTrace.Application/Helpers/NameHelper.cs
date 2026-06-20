using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Helpers
{
    public static class NameHelper
    {
        public static string CombineNames(
            string? firstName,
            string? secondName = null,
            string? thirdName = null,
            string? fourthName = null,
            string defaultValue = "Unknown")
        {
            var fullName = string.Join(" ",
                new[] { firstName, secondName, thirdName, fourthName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            return string.IsNullOrWhiteSpace(fullName)
                ? defaultValue
                : fullName;
        }
    }
}
