namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>
    /// Ensures a list, when provided, contains at least the specified number of elements.
    /// Null lists pass (use [Required] to reject nulls separately).
    /// </summary>
    public class MinListLengthAttribute : ValidationAttribute
    {
        private readonly int _min;
        public MinListLengthAttribute(int min) => _min = min;

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is System.Collections.ICollection col)
                return col.Count >= _min;
            return false;
        }
    }
}
