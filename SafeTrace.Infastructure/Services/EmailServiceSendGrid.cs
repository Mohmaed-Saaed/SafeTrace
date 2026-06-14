using Microsoft.Extensions.Configuration;
using SafeTrace.Application.Interfaces.IServices;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SafeTrace.Infrastructure.Services
{
    public class EmailServiceSendGrid : IEmailServiceSendGrid
    {
        private readonly IConfiguration _configuration;
        public EmailServiceSendGrid(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid API key is not configured.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("SendGrid sender email is not configured.");

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: StripHtml(body),
                htmlContent: body);

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Failed to send email via SendGrid. Status: {(int)response.StatusCode}, Body: {errorBody}");
            }
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}