using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterAzureFunction.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendDetentionLetterEmailAsync(Contact primaryRecipient, List<Contact> ccRecipients, List<byte[]> attachments, string orderNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(primaryRecipient?.Email))
                {
                    _logger.LogWarning($"Cannot send email - primary recipient email is null for order {orderNumber}");
                    return false;
                }

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var subject = $"Important Detention Letter Information - Order {orderNumber}";
                var body = $@"
                    <html>
                    <body>
                        <p>Dear {primaryRecipient.FullName},</p>
                        <p>Please find attached important detention letter information for your order {orderNumber}.</p>
                        <p>If you have any questions, please contact your sales engineer.</p>
                        <p>Best regards,<br/>The Team</p>
                    </body>
                    </html>";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(primaryRecipient.Email);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    var uniqueCcEmails = ccRecipients
                        .Where(c => !string.IsNullOrEmpty(c.Email))
                        .Select(c => c.Email)
                        .Distinct()
                        .Where(email => !email.Equals(primaryRecipient.Email, StringComparison.OrdinalIgnoreCase));

                    foreach (var email in uniqueCcEmails)
                    {
                        message.CC.Add(email);
                    }

                    for (int i = 0; i < attachments.Count; i++)
                    {
                        if (attachments[i] != null && attachments[i].Length > 0)
                        {
                            var stream = new MemoryStream(attachments[i]);
                            var attachment = new Attachment(stream, $"DetentionLetter_{i + 1}.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                            message.Attachments.Add(attachment);
                        }
                    }

                    using (var smtpClient = CreateSmtpClient())
                    {
                        await smtpClient.SendMailAsync(message);
                    }
                }

                _logger.LogInformation($"Email sent successfully for order {orderNumber} to {primaryRecipient.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email for order {orderNumber}");
                return false;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

            var smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

            return smtpClient;
        }
    }
}
