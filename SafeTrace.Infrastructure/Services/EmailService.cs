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
                throw new BadRequestException("البريد الإلكتروني للمستلم غير صالح أو مفقود.");

            if (string.IsNullOrWhiteSpace(_mailSettings.Host) || string.IsNullOrWhiteSpace(_mailSettings.Email))
                throw new BadRequestException("إعدادات خادم إرسال البريد الإلكتروني (SMTP) غير مكتملة. يرجى مراجعة الدعم الفني.");

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
                throw new UnauthorizedException("فشلت عملية المصادقة مع خادم البريد الإلكتروني. يرجى التأكد من إعدادات الإرسال.");
            }
            catch (SmtpCommandException ex)
            {
                throw new BadRequestException("رفض خادم البريد الإلكتروني إرسال الرسالة.");
            }
            catch (SmtpProtocolException)
            {
                throw new BadRequestException("حدث خطأ في بروتوكول الاتصال بخادم البريد الإلكتروني.");
            }
            catch (SocketException)
            {
                throw new BadRequestException("تعذر الوصول إلى خادم البريد الإلكتروني. يرجى التحقق من اتصالك بالإنترنت.");
            }
            catch (Exception)
            {
                throw new BadRequestException("حدث خطأ غير متوقع أثناء محاولة إرسال البريد الإلكتروني.");
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