using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SecureApi.Models;
using System.Net;
using System.Net.Mail;

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
            string htmlMessage,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required");

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.From, _settings.DisplayName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            try
            {
                using var smtp = new SmtpClient(_settings.Host, _settings.Port)
                {
                    Credentials = new NetworkCredential(
                        _settings.Username,
                        _settings.Password),
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtp.SendMailAsync(mail, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email to {Email}. Subject: {Subject}",
                    toEmail, subject);

                throw; // Let caller decide what to do
            }
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }
    }
}
