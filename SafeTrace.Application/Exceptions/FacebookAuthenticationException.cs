namespace SafeTrace.Application.Exceptions
{
    public sealed class FacebookAuthenticationException : Exception
    {
        public FacebookAuthenticationException(string message)
            : base(message)
        {
        }
    }
}
