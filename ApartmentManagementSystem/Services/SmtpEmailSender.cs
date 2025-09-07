using System.Net;
using System.Net.Mail;

namespace ApartmentManagementSystem.Services
{
    public class SmtpEmailSender : IEmailSenderService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, string? fromName = null, string? fromAddress = null, CancellationToken ct = default)
        {
            var emailSection = _config.GetSection("Email");
            var smtpSection = emailSection.GetSection("Smtp");
            var fromAddr = fromAddress ?? emailSection["FromAddress"] ?? "noreply@localhost";
            var fromNm = fromName ?? emailSection["FromName"] ?? "Apartment Management";

            using var msg = new MailMessage
            {
                From = new MailAddress(fromAddr, fromNm),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            using var client = new SmtpClient
            {
                Host = smtpSection["Host"]!,
                Port = int.Parse(smtpSection["Port"] ?? "587"),
                EnableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"])
            };

            try
            {
                await client.SendMailAsync(msg, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
                // swallow to avoid failing the request; log only
            }
        }
    }
}
