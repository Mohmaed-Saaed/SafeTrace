using System;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Ensures every ID in a List&lt;long&gt; is a positive number.</summary>
    public class PositiveIdsAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is List<long> ids)
                return ids.All(id => id > 0);
            return false;
        }
    }
}
