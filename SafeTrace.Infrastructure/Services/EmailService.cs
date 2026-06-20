using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Options;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SafeTrace.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettingsOptions _mailSettings;

        public EmailService(IOptions<MailSettingsOptions> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new BadRequestException("Recipient address payload is missing or invalid.");

            if (string.IsNullOrWhiteSpace(_mailSettings.Host) || string.IsNullOrWhiteSpace(_mailSettings.Email))
                throw new InvalidOperationException("SMTP runtime infrastructure configurations are incomplete.");

            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Email);
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body,
                TextBody = StripHtml(body)
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            try
            {
                await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_mailSettings.Email, _mailSettings.Password);

                await smtp.SendAsync(email);
            }
            catch (AuthenticationException)
            {
                throw new UnauthorizedException("Mail gateway authentication rejected. Verification credentials mismatched.");
            }
            catch (SmtpCommandException ex)
            {
                throw new BadRequestException($"Mail gateway refused the operational command. Status Code: {ex.StatusCode}");
            }
            catch (SmtpProtocolException)
            {
                throw new BadRequestException("Mail gateway communication protocol sequence failed.");
            }
            catch (SocketException)
            {
                throw new BadRequestException("Mail gateway is unreachable. Connection timed out.");
            }
            catch (Exception)
            {
                throw new BadRequestException("Critical network interruption during secure mail dispatch.");
            }
            finally
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true);
            }
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}