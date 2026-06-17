using Microsoft.Extensions.Options;
using SafeTrace.Application.Exceptions;
using SafeTrace.Infrastructure.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace SafeTrace.Infrastructure.Services
{
    public class EmailServiceSendGrid : IEmailServiceSendGrid
    {
        private readonly SendGridOptions _sendGridOptions;

        public EmailServiceSendGrid(IOptions<SendGridOptions> sendGridOptions)
        {
            _sendGridOptions = sendGridOptions.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new BadRequestException("Recipient email address cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(_sendGridOptions.ApiKey))
                throw new InvalidOperationException("SendGrid runtime API infrastructure key is not configured.");

            if (string.IsNullOrWhiteSpace(_sendGridOptions.FromEmail))
                throw new InvalidOperationException("SendGrid verified sender identity email address is missing.");

            var client = new SendGridClient(_sendGridOptions.ApiKey);
            var from = new EmailAddress(_sendGridOptions.FromEmail, _sendGridOptions.FromName);
            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: StripHtml(body),
                htmlContent: body);

            try
            {
                var response = await client.SendEmailAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new UnauthorizedException("SendGrid mail dispatch request rejected due to invalid API key permission references.");

                    throw new BadRequestException($"External mail delivery network failure.");
                }
            }
            catch (Exception ex) when (ex is not BadRequestException && ex is not UnauthorizedException)
            {
                throw new BadRequestException($"Critical gateway time-out or network drop during remote mail dispatch.");
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