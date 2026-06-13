using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoStock.Services.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<ServiceResult<bool>> SendAsync(
            EmailMessageDto message,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
                return ServiceResult<bool>.Success(true);

            if (string.IsNullOrWhiteSpace(message.ToEmail))
                return ServiceResult<bool>.Fail("Alıcı e-posta adresi boş olamaz.");

            if (string.IsNullOrWhiteSpace(_settings.Host))
                return ServiceResult<bool>.Fail("SMTP host ayarı eksik.");

            if (string.IsNullOrWhiteSpace(_settings.UserName))
                return ServiceResult<bool>.Fail("SMTP kullanıcı adı eksik.");

            if (string.IsNullOrWhiteSpace(_settings.Password))
                return ServiceResult<bool>.Fail("SMTP şifresi eksik.");

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                return ServiceResult<bool>.Fail("Gönderen e-posta adresi eksik.");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var mail = new MailMessage();

                mail.From = new MailAddress(
                    _settings.FromEmail,
                    string.IsNullOrWhiteSpace(_settings.FromName)
                        ? _settings.FromEmail
                        : _settings.FromName,
                    Encoding.UTF8);

                mail.To.Add(new MailAddress(
                    message.ToEmail,
                    message.ToName,
                    Encoding.UTF8));

                mail.Subject = message.Subject;
                mail.SubjectEncoding = Encoding.UTF8;
                mail.BodyEncoding = Encoding.UTF8;
                mail.HeadersEncoding = Encoding.UTF8;

                mail.Headers.Add("Message-ID", CreateMessageId(_settings.FromEmail));
                mail.Headers.Add("X-Auto-Response-Suppress", "All");
                mail.Headers.Add("List-Unsubscribe", $"<mailto:{_settings.FromEmail}>");

                var plainTextBody = CreatePlainTextBody(message.HtmlBody);

                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    plainTextBody,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Plain);

                var htmlView = AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Html);

                mail.AlternateViews.Add(plainTextView);
                mail.AlternateViews.Add(htmlView);

                if (message.Attachments is not null)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment.Content is null || attachment.Content.Length == 0)
                            continue;

                        var stream = new MemoryStream(attachment.Content);

                        var mailAttachment = new Attachment(
                            stream,
                            attachment.FileName,
                            attachment.ContentType);

                        mail.Attachments.Add(mailAttachment);
                    }
                }

                using var smtp = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(
                        _settings.UserName,
                        _settings.Password),
                    Timeout = Math.Max(_settings.TimeoutSeconds, 10) * 1000
                };

                await smtp.SendMailAsync(mail, cancellationToken);

                return ServiceResult<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<bool>.Fail("E-posta gönderimi iptal edildi.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"E-posta gönderilemedi: {ex.Message}");
            }
        }

        private static string CreateMessageId(string fromEmail)
        {
            var domain = fromEmail.Contains('@')
                ? fromEmail.Split('@').Last()
                : "sente360.com";

            return $"<{Guid.NewGuid():N}.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}@{domain}>";
        }

        private static string CreatePlainTextBody(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var text = html;

            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</tr\s*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</td\s*>", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", " ");
            text = WebUtility.HtmlDecode(text);

            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n\s+\n", "\n\n");

            return text.Trim();
        }
    }
}