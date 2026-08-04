using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.Options;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SafeTrace.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettingsOptions _mailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettingsOptions> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Email sending failed: Recipient email is missing or invalid.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_mailSettings.Host) || string.IsNullOrWhiteSpace(_mailSettings.Email))
            {
                _logger.LogError("Email sending failed: SMTP settings are incomplete.");
                return;
            }

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

            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;


            try
            {
                await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_mailSettings.Email, _mailSettings.Password);

                await smtp.SendAsync(email);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication failed with the SMTP server.");
                throw new InvalidOperationException("فشلت عملية المصادقة مع خادم البريد الإلكتروني.", ex);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP server rejected the message.");
                throw new InvalidOperationException("رفض خادم البريد الإلكتروني إرسال الرسالة.", ex);
            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error occurred.");
                throw new InvalidOperationException("حدث خطأ في بروتوكول الاتصال بخادم البريد الإلكتروني.", ex);
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Could not connect to the SMTP server.");
                throw new InvalidOperationException("تعذر الوصول إلى خادم البريد الإلكتروني.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending email.");
                throw new InvalidOperationException("حدث خطأ غير متوقع أثناء محاولة إرسال البريد الإلكتروني.", ex);
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