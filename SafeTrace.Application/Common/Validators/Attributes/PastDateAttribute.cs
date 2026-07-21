namespace SafeTrace.Application.Common.Validators.Attributes
{
    public class PastDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            return value switch
            {
                DateTime dt => dt <= DateTime.UtcNow,
                DateOnly date => date <= DateOnly.FromDateTime(DateTime.UtcNow),
                _ => false
            };
        }
    }
}