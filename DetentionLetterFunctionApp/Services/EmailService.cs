using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterFunctionApp.Services
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

        public async Task<bool> SendDetentionLetterEmailAsync(OrderSummary orderSummary, List<User> ccUsers, List<string> attachmentPaths)
        {
            try
            {
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var toEmail = orderSummary.SoldToEmail;
                var subject = $"Important Installation Procedures: {orderSummary.OrderNumber}: {orderSummary.OrderName} – {orderSummary.City}, {orderSummary.State}";
                var ccEmails = string.Join(";", ccUsers.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).Distinct());
                var body = "Please find attached critical documents that provide the proper procedures for installation and backfilling of material your company recently ordered from Contech. Please contact your local sales engineer if you have specific questions related to the installation process.";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;
                    message.Priority = MailPriority.High;

                    if (!string.IsNullOrEmpty(ccEmails))
                    {
                        foreach (var email in ccEmails.Split(';'))
                        {
                            if (!string.IsNullOrEmpty(email))
                            {
                                message.CC.Add(email.Trim());
                            }
                        }
                    }

                    foreach (var attachmentPath in attachmentPaths)
                    {
                        if (File.Exists(attachmentPath))
                        {
                            message.Attachments.Add(new Attachment(attachmentPath));
                        }
                    }

                    using (var smtpClient = CreateSmtpClient())
                    {
                        await smtpClient.SendMailAsync(message);
                    }
                }

                _logger.LogInformation($"Email sent successfully for order {orderSummary.OrderNumber}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email for order {orderSummary.OrderNumber}");
                return false;
            }
        }

        public async Task SendMissingEmailNotificationAsync(User user, string orderNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(user?.Email))
                {
                    _logger.LogWarning($"Cannot send missing email notification - user email is null for order {orderNumber}");
                    return;
                }

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var subject = $"ALERT: Large Diameter/Detention Letter was NOT sent for: {orderNumber}";
                var body = "The Sold To Contact associated to this Order does not have an email address. You must manually create and send the appropriate letter and attachment to the customer. Please update the contact's email address in CRM so it is available for the next order.";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(user.Email);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var smtpClient = CreateSmtpClient())
                    {
                        await smtpClient.SendMailAsync(message);
                    }
                }

                _logger.LogInformation($"Missing email notification sent to {user.Email} for order {orderNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending missing email notification for order {orderNumber}");
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
