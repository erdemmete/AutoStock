using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using Microsoft.Extensions.Logging;
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
        private const string EmailNotConfiguredMessage = "E-posta gönderimi yapılandırılmamış. Lütfen sistem yöneticisiyle iletişime geçin.";
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailSettings> options,
            ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> SendAsync(
            EmailMessageDto message,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogWarning("Email sending requested but EmailSettings.Enabled is false. ToEmail: {ToEmail}, Subject: {Subject}",
                    message.ToEmail,
                    message.Subject);

                return ServiceResult<bool>.Fail(EmailNotConfiguredMessage);
            }

            if (string.IsNullOrWhiteSpace(message.ToEmail))
                return ServiceResult<bool>.Fail("Alıcı e-posta adresi boş olamaz.");

            var configurationErrors = GetConfigurationErrors();

            if (configurationErrors.Any())
            {
                _logger.LogError(
                    "Email sending requested with incomplete SMTP configuration. MissingFields: {MissingFields}, ToEmail: {ToEmail}, Subject: {Subject}",
                    string.Join(", ", configurationErrors),
                    message.ToEmail,
                    message.Subject);

                return ServiceResult<bool>.Fail(EmailNotConfiguredMessage);
            }

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

                _logger.LogInformation(
                    "Email sent successfully. ToEmail: {ToEmail}, Subject: {Subject}, FromEmail: {FromEmail}, Host: {Host}, Port: {Port}",
                    message.ToEmail,
                    message.Subject,
                    _settings.FromEmail,
                    _settings.Host,
                    _settings.Port);

                return ServiceResult<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Email sending cancelled or timed out. ToEmail: {ToEmail}, Subject: {Subject}, Host: {Host}, Port: {Port}, TimeoutSeconds: {TimeoutSeconds}",
                    message.ToEmail,
                    message.Subject,
                    _settings.Host,
                    _settings.Port,
                    _settings.TimeoutSeconds);

                return ServiceResult<bool>.Fail("E-posta gönderilemedi. Lütfen kısa süre sonra tekrar deneyin.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email sending failed. ToEmail: {ToEmail}, Subject: {Subject}, Host: {Host}, Port: {Port}, EnableSsl: {EnableSsl}, UserName: {UserName}, FromEmail: {FromEmail}",
                    message.ToEmail,
                    message.Subject,
                    _settings.Host,
                    _settings.Port,
                    _settings.EnableSsl,
                    _settings.UserName,
                    _settings.FromEmail);

                return ServiceResult<bool>.Fail("E-posta gönderilemedi. Lütfen kısa süre sonra tekrar deneyin.");
            }
        }

        private List<string> GetConfigurationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.Host))
                errors.Add(nameof(_settings.Host));

            if (_settings.Port <= 0)
                errors.Add(nameof(_settings.Port));

            if (string.IsNullOrWhiteSpace(_settings.UserName))
                errors.Add(nameof(_settings.UserName));

            if (string.IsNullOrWhiteSpace(_settings.Password))
                errors.Add(nameof(_settings.Password));

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                errors.Add(nameof(_settings.FromEmail));

            return errors;
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
