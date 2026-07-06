namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>Ensures a DateTime value is not in the future.</summary>
    public class PastDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null) return true; // let [Required] handle nulls

            if (value is DateTime dt)
                return dt <= DateTime.UtcNow;

            return false;
        }
    }
}