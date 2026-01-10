using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureApi.Models;

namespace SecureApi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IOptions<SmtpSettings> settings,
            ILogger<EmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage)
        {
            await SendEmailAsync(toEmail, subject, htmlMessage, CancellationToken.None);
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject is required", nameof(subject));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage,
                TextBody = ConvertHtmlToPlainText(htmlMessage)
            };

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();

                // Connect to SMTP server
                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    GetSecureSocketOptions(_settings.EnableSsl, _settings.Port),
                    cancellationToken);

                // Authenticate
                if (!string.IsNullOrWhiteSpace(_settings.Username) &&
                    !string.IsNullOrWhiteSpace(_settings.Password))
                {
                    await client.AuthenticateAsync(
                        _settings.Username,
                        _settings.Password,
                        cancellationToken);
                }

                // Send email
                await client.SendAsync(message, cancellationToken);

                // Disconnect
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation(
                    "Email sent successfully to {Email}. Subject: {Subject}",
                    toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email to {Email}. Subject: {Subject}",
                    toEmail, subject);

                throw;
            }
        }

        /// <summary>
        /// Determines the appropriate SSL/TLS options based on settings
        /// </summary>
        private static SecureSocketOptions GetSecureSocketOptions(bool enableSsl, int port)
        {
            if (!enableSsl)
                return SecureSocketOptions.None;

            // Port 465 typically uses implicit SSL, port 587 uses STARTTLS
            return port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
        }

        /// <summary>
        /// Simple HTML to plain text converter for email fallback
        /// </summary>
        private static string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove HTML tags
            var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");

            // Replace common HTML entities
            plainText = plainText
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"");

            // Normalize whitespace
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");

            return plainText.Trim();
        }
    }
}