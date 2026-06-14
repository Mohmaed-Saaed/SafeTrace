namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IEmailServiceSendGrid
    {
        public Task SendEmailAsync(string toEmail, string subject, string body);
    }
}