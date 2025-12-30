using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;
using Microsoft.Extensions.Logging;

namespace DetentionLetterFunctionApp.Services
{
    public class DetentionLetterService : IDetentionLetterService
    {
        private readonly IDynamics365Service _dynamics365Service;
        private readonly IReportService _reportService;
        private readonly IEmailService _emailService;
        private readonly ILogger<DetentionLetterService> _logger;

        public DetentionLetterService(
            IDynamics365Service dynamics365Service,
            IReportService reportService,
            IEmailService emailService,
            ILogger<DetentionLetterService> logger)
        {
            _dynamics365Service = dynamics365Service;
            _reportService = reportService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ProcessPendingDetentionLettersAsync()
        {
            var pendingOrders = await _dynamics365Service.GetPendingDetentionLettersAsync();
            _logger.LogInformation($"Found {pendingOrders.Count} pending detention letters to process");

            foreach (var order in pendingOrders)
            {
                await ProcessOrderAsync(order);
            }
        }

        private async Task ProcessOrderAsync(OrderSummary order)
        {
            try
            {
                _logger.LogInformation($"Processing order: {order.OrderNumber}");

                var ccUsers = new List<User>();
                var modifiedByUser = await _dynamics365Service.GetUserByIdAsync(order.OrderModifiedBy);
                if (modifiedByUser != null)
                {
                    ccUsers.Add(modifiedByUser);
                }

                var reportTypes = await _dynamics365Service.GetReportListForOrderAsync(order.OrderId.ToString());
                _logger.LogInformation($"Order {order.OrderNumber}: Found {reportTypes.Count} report types");

                if (reportTypes.Count == 0)
                {
                    await _dynamics365Service.UpdateOrderSummaryStatusAsync(order.SummaryId, true, "Not Qualified for Letter");
                    _logger.LogInformation($"Order {order.OrderNumber} not qualified for letter");
                    return;
                }

                foreach (var reportType in reportTypes)
                {
                    var orderHistory = new OrderHistory
                    {
                        HistoryId = Guid.NewGuid(),
                        SummaryId = order.SummaryId,
                        AssociatedToOrderId = order.OrderId,
                        ReportName = reportType.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        Status = "Pending"
                    };

                    await _dynamics365Service.CreateOrderHistoryAsync(orderHistory);
                }

                var historyRecords = await _dynamics365Service.GetOrderHistoryAsync(order.SummaryId);
                var attachmentPaths = new List<string>();

                foreach (var history in historyRecords)
                {
                    var reportData = await _reportService.GenerateReportAsync(
                        history.AssociatedToOrderId,
                        history.ReportName,
                        order.OrderModifiedBy);

                    if (reportData != null && reportData.Length > 0)
                    {
                        var fileName = $"{history.ReportName}_{order.OrderNumber}.pdf";
                        var filePath = Path.Combine(order.DocumentPath ?? Path.GetTempPath(), fileName);

                        var saved = await _reportService.SaveReportToFileAsync(reportData, filePath);
                        if (saved)
                        {
                            history.AttachmentPath = filePath;
                            attachmentPaths.Add(filePath);

                            await _dynamics365Service.UpdateOrderHistoryAsync(
                                history.HistoryId,
                                true,
                                reportData.Length,
                                "Report generated successfully");

                            if (Enum.TryParse<LettersType>(history.ReportName, out var letterType))
                            {
                                var additionalDocs = _reportService.GetAdditionalDocumentsForReport(letterType);
                                attachmentPaths.AddRange(additionalDocs.Where(File.Exists));
                            }
                        }
                    }
                }

                if (attachmentPaths.Count > 0)
                {
                    if (IsValidEmailAddress(order.SoldToEmail))
                    {
                        var salesEngineer = await _dynamics365Service.GetSalesEngineerByOrderNumberAsync(order.OrderNumber);
                        if (salesEngineer != null)
                        {
                            ccUsers.Add(salesEngineer);
                        }

                        var emailSent = await _emailService.SendDetentionLetterEmailAsync(order, ccUsers, attachmentPaths);

                        if (emailSent)
                        {
                            await _dynamics365Service.UpdateOrderSummaryStatusAsync(order.SummaryId, true, "Successful");
                            _logger.LogInformation($"Successfully processed and sent email for order: {order.OrderNumber}");
                        }
                        else
                        {
                            await _dynamics365Service.UpdateOrderSummaryStatusAsync(order.SummaryId, false, "Email sending failed");
                            _logger.LogWarning($"Failed to send email for order: {order.OrderNumber}");
                        }
                    }
                    else
                    {
                        await _dynamics365Service.UpdateOrderSummaryStatusAsync(
                            order.SummaryId,
                            true,
                            "SoldTo Email is not exist or Invalid for Sold to Contact");

                        if (modifiedByUser != null)
                        {
                            await _emailService.SendMissingEmailNotificationAsync(modifiedByUser, order.OrderNumber);
                        }

                        _logger.LogWarning($"Invalid email for order {order.OrderNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing order {order.OrderNumber}");
                await _dynamics365Service.UpdateOrderSummaryStatusAsync(order.SummaryId, false, $"Error: {ex.Message}");
            }
        }

        private bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailValidator = new EmailAddressAttribute();
            return emailValidator.IsValid(email);
        }
    }
}
