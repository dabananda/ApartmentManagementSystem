using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace ApartmentManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpServer = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"]);
            var senderEmail = _config["Smtp:From"];
            var username = _config["Smtp:User"];
            var password = _config["Smtp:Password"];

            if (string.IsNullOrWhiteSpace(smtpServer) || port <= 0 ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP settings are missing or invalid. Check Smtp:Host/Port/From/User/Password.");
            }

            using var client = new SmtpClient(smtpServer, port);

            // CRITICAL: Set UseDefaultCredentials to false BEFORE setting credentials
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(username, password);
            client.EnableSsl = true;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            using var mailMessage = new MailMessage(senderEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}