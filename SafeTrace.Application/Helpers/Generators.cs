namespace SafeTrace.Application.Helpers
{
    public static class Generators
    {
        public static string GenerateCaseCode()
        {
            return $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }
    }
}