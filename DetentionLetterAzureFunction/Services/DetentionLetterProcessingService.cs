using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterAzureFunction.Services
{
    public class DetentionLetterProcessingService : IDetentionLetterProcessingService
    {
        private readonly IDataverseService _dataverseService;
        private readonly ILetterDeterminationService _letterDeterminationService;
        private readonly IReportService _reportService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DetentionLetterProcessingService> _logger;
        private readonly Guid _requiredBusinessUnitId;

        public DetentionLetterProcessingService(
            IDataverseService dataverseService,
            ILetterDeterminationService letterDeterminationService,
            IReportService reportService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<DetentionLetterProcessingService> logger)
        {
            _dataverseService = dataverseService;
            _letterDeterminationService = letterDeterminationService;
            _reportService = reportService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _requiredBusinessUnitId = Guid.Parse(_configuration["Dynamics365:BusinessUnitId"]);
        }

        public async Task ProcessPendingDetentionLettersAsync()
        {
            try
            {
                _logger.LogInformation("Starting detention letter processing");

                var pendingOrders = await _dataverseService.GetPendingDetentionOrdersAsync();
                _logger.LogInformation($"Found {pendingOrders.Count} pending detention orders");

                foreach (var detentionOrder in pendingOrders)
                {
                    await ProcessDetentionOrderAsync(detentionOrder);
                }

                _logger.LogInformation("Completed detention letter processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPendingDetentionLettersAsync");
            }
        }

        private async Task ProcessDetentionOrderAsync(DetentionOrderSummary detentionOrder)
        {
            try
            {
                _logger.LogInformation($"Processing detention order {detentionOrder.Id}");

                if (detentionOrder.OwningBusinessUnitId != _requiredBusinessUnitId)
                {
                    _logger.LogInformation($"Skipping detention order {detentionOrder.Id} - Business Unit does not match");
                    return;
                }

                var salesOrder = await _dataverseService.GetSalesOrderAsync(detentionOrder.SalesOrderId);
                if (salesOrder == null)
                {
                    _logger.LogWarning($"Sales order not found for detention order {detentionOrder.Id}");
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, "Sales order not found");
                    return;
                }

                var orderProducts = await _dataverseService.GetOrderProductsAsync(salesOrder.Id);
                if (orderProducts == null || orderProducts.Count == 0)
                {
                    _logger.LogWarning($"No products found for sales order {salesOrder.OrderNumber}");
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, "No products found");
                    return;
                }

                var letterTypes = _letterDeterminationService.DetermineLetterTypes(orderProducts);
                if (letterTypes.Count == 0)
                {
                    _logger.LogInformation($"No detention letters required for order {salesOrder.OrderNumber}");
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 1, "No detention letters required");
                    return;
                }

                var reports = new List<byte[]>();
                foreach (var letterType in letterTypes)
                {
                    var report = await _reportService.GenerateWordReportAsync(salesOrder.Id, letterType);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }

                if (reports.Count == 0)
                {
                    _logger.LogWarning($"Failed to generate reports for order {salesOrder.OrderNumber}");
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, "Report generation failed");
                    return;
                }

                var soldToContact = await _dataverseService.GetContactAsync(detentionOrder.SoldToContactId);
                if (soldToContact == null || string.IsNullOrEmpty(soldToContact.Email))
                {
                    _logger.LogWarning($"Sold-To contact email not found for order {salesOrder.OrderNumber}");
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, "Sold-To contact email not found");
                    return;
                }

                var salesEngineers = await _dataverseService.GetSalesEngineersAsync(salesOrder.ZipCode);

                var emailSent = await _emailService.SendDetentionLetterEmailAsync(
                    soldToContact,
                    salesEngineers,
                    reports,
                    salesOrder.OrderNumber);

                if (emailSent)
                {
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 1);
                    _logger.LogInformation($"Successfully processed detention order {detentionOrder.Id} for order {salesOrder.OrderNumber}");
                }
                else
                {
                    await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, "Email sending failed");
                    _logger.LogWarning($"Failed to send email for order {salesOrder.OrderNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing detention order {detentionOrder.Id}");
                await _dataverseService.UpdateDetentionOrderStatusAsync(detentionOrder.Id, 0, ex.Message);
            }
        }
    }
}
