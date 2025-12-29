using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Xrm.Sdk;
using BL = CONTECH.Service.BusinessLogic;
using BE = CONTECH.Service.BusinessEntities;

namespace DetentionLetterAutomationService.Plugins
{
    /// <summary>
    /// Plugin for processing detention letters in Dynamics 365
    /// This plugin should be registered on:
    /// - Entity: salesorder (or custom entity)
    /// - Message: Create, Update, or custom message
    /// - Stage: Post-operation (Asynchronous recommended)
    ///
    /// For batch processing, register on a custom message or scheduled workflow
    /// </summary>
    public class DetentionLetterPlugin : PluginBase
    {
        public DetentionLetterPlugin() : base(typeof(DetentionLetterPlugin))
        {
        }

        protected override void ExecutePlugin(
            ITracingService tracingService,
            IPluginExecutionContext context,
            IOrganizationService service,
            IOrganizationServiceFactory serviceFactory)
        {
            tracingService.Trace("Starting Detention Letter Processing");

            try
            {
                // Check the depth to prevent infinite loops
                if (context.Depth > 1)
                {
                    tracingService.Trace("Context depth > 1, exiting to prevent recursion");
                    return;
                }

                // Process detention letters
                ProcessDetentionLetters(tracingService, service);

                tracingService.Trace("Detention Letter Processing completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error in DetentionLetterPlugin: {ex.Message}");
                Logger.Error($"{DateTime.Today.ToLongDateString()} Exception in DetentionLetterPlugin - {ex.StackTrace}");
                Logger.Error($"Message ------ {ex.Message}");
                throw new InvalidPluginExecutionException($"Error processing detention letters: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Main business logic for processing detention letters
        /// Converted from the _aTimer_Elapsed method in the Windows Service
        /// </summary>
        private void ProcessDetentionLetters(ITracingService tracingService, IOrganizationService service)
        {
            tracingService.Trace("ProcessDetentionLetters - Start");

            BL.DetentionLetterSummaryAction dLtrrAct = new BL.DetentionLetterSummaryAction();
            List<BE.OrderSummary> lstHistoryIds = dLtrrAct.GetPendingDetentionLetter();

            tracingService.Trace($"Found {lstHistoryIds.Count} pending detention letters");
            Logger.Info($"Found {lstHistoryIds.Count} pending detention letters");

            if (lstHistoryIds.Count > 0)
            {
                foreach (BE.OrderSummary dlOrderSummary in lstHistoryIds)
                {
                    try
                    {
                        ProcessSingleOrder(tracingService, dLtrrAct, dlOrderSummary);
                    }
                    catch (Exception ex)
                    {
                        tracingService.Trace($"Error processing order {dlOrderSummary.OrderNumber}: {ex.Message}");
                        Logger.Error($"Exception processing order {dlOrderSummary.OrderNumber} - {ex.StackTrace}");
                        Logger.Error($"Message ------ {ex.Message}");
                        // Continue processing other orders
                    }
                }
            }

            tracingService.Trace("ProcessDetentionLetters - End");
        }

        /// <summary>
        /// Process a single order's detention letter
        /// </summary>
        private void ProcessSingleOrder(
            ITracingService tracingService,
            BL.DetentionLetterSummaryAction dLtrrAct,
            BE.OrderSummary dlOrderSummary)
        {
            List<BE.Users> lstUser = new List<BE.Users>();

            // Get Modified User and Add into CC UserList
            lstUser.Add(dLtrrAct.GetUserDetail(dlOrderSummary.OrderModifiedBy));

            string salesorderid = Convert.ToString(dlOrderSummary.OrderId);
            string _orderNumber = dlOrderSummary.OrderNumber;

            tracingService.Trace($"Processing Order: {_orderNumber}");
            Logger.Info($"Processing Order: {_orderNumber}");

            // Get Detention Report list using spec #602 condition
            BL.GetDetentionLetterReport reportList = new BL.GetDetentionLetterReport();
            List<BL.LettersType> lstReport = reportList.GetReportListForDownload(salesorderid);

            tracingService.Trace($"{_orderNumber} - Letters to send: {String.Join(",", lstReport)}");
            Logger.Info($"{_orderNumber} - Send Letters: {String.Join(",", lstReport)}");

            DetentionLetterReport dlr = new DetentionLetterReport();

            // Insert History record according to report type
            foreach (BL.LettersType detentionletter in lstReport)
            {
                dlr.InsertOrderHistory(dlOrderSummary, detentionletter.ToString());
            }

            tracingService.Trace($"{_orderNumber}: Insert History record according to report type");
            Logger.Info($"{_orderNumber}: Insert History record according to report type");

            if (lstReport.Count > 0)
            {
                // Copy Detention letter on document path of order
                dlr.GenerateReport(dlOrderSummary);
                tracingService.Trace($"{_orderNumber}: Copy Detention letter on document path of order");
                Logger.Info($"{_orderNumber}: Copy Detention letter on document path of order");

                if (IsValidEmailAddress(dlOrderSummary.SoldToEmail))
                {
                    try
                    {
                        // Get SESell1 User and Add into CC Userlist
                        lstUser.Add(dLtrrAct.GetSESell1Detail(dlOrderSummary.OrderNumber));

                        // Copy Detention letter from document path and attach to email and send it
                        bool isSendEmail = dlr.SendReportEmail(dlOrderSummary, lstUser);

                        tracingService.Trace($"{_orderNumber}: Copy Detention letter from document path and attach to email and send it");
                        Logger.Info($"{_orderNumber}: Copy Detention letter from document path and attach to email and send it");

                        if (isSendEmail)
                        {
                            // Update Send status and message
                            dlr.UpdateStatus(dlOrderSummary, true, "Successful");
                            tracingService.Trace($"{_orderNumber}: Update Send status and Send Summary Report");
                            Logger.Info($"{_orderNumber}: Update Send status and Send Summary Report");
                        }
                    }
                    catch (Exception ex)
                    {
                        tracingService.Trace($"Error sending email for order {_orderNumber}: {ex.Message}");
                        Logger.Error($"Exception in sending email for order {_orderNumber} - {ex.StackTrace}");
                        Logger.Error($"Message ------ {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    // Update Send status and message
                    dlr.UpdateStatus(dlOrderSummary, true, "SoldTo Email is not exist or Invalid for Sold to Contact.");
                    dlr.SendMissingDetailEmail(dlOrderSummary.OrderModifiedBy, dlOrderSummary.OrderNumber);

                    tracingService.Trace($"{_orderNumber}: Update Status and Send Email to order User.");
                    Logger.Info($"{_orderNumber}: Update Status and Send Email to order User.");
                }
            }
            else
            {
                // Update message qualified for send report or not
                dlr.UpdateStatus(dlOrderSummary, true, "Not Qualified for Letter");
                tracingService.Trace($"{_orderNumber}: Not Qualified for Letter");
                Logger.Info($"{_orderNumber}: Not Qualified for Letter");
            }
        }

        /// <summary>
        /// Validate email address
        /// </summary>
        private bool IsValidEmailAddress(string address)
        {
            EmailAddressAttribute emailAttr = new EmailAddressAttribute();

            if (address != null && emailAttr.IsValid(address))
                return true;

            return false;
        }
    }
}
