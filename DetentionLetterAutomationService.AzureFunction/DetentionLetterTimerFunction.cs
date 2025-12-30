using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using BL = CONTECH.Service.BusinessLogic;
using BE = CONTECH.Service.BusinessEntities;

namespace DetentionLetterAutomationService.AzureFunction
{
    public class DetentionLetterTimerFunction
    {
        private readonly ILogger _logger;

        public DetentionLetterTimerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DetentionLetterTimerFunction>();
        }

        /// <summary>
        /// Timer trigger function that runs every 10 minutes (configurable via TimerSchedule app setting)
        /// NCRONTAB format: "0 */10 * * * *" = every 10 minutes
        /// To run every 2 minutes: "0 */2 * * * *"
        /// To run daily at 2:00 AM: "0 0 2 * * *"
        /// </summary>
        [Function("DetentionLetterTimerFunction")]
        public void Run([TimerTrigger("%TimerSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("Timer trigger function executed at: {time}", DateTime.Now);

            try
            {
                BL.DetentionLetterSummaryAction dLtrrAct = new BL.DetentionLetterSummaryAction();
                List<BE.OrderSummary> lstHistoryIds = dLtrrAct.GetPendingDetentionLetter();

                if (lstHistoryIds.Count > 0)
                {
                    _logger.LogInformation("Found {count} pending detention letters to process", lstHistoryIds.Count);

                    foreach (BE.OrderSummary dlOrderSummary in lstHistoryIds)
                    {
                        List<BE.Users> lstUser = new List<BE.Users>();
                        //Get Modified User and Add into CC UserList
                        lstUser.Add(dLtrrAct.GetUserDetail(dlOrderSummary.OrderModifiedBy));
                        string salesorderid = Convert.ToString(dlOrderSummary.OrderId);
                        string _orderNumber = dlOrderSummary.OrderNumber;

                        _logger.LogInformation("Processing order: {orderNumber}", _orderNumber);

                        //Get Detention Report list using spec #602 condition
                        BL.GetDetentionLetterReport reportList = new BL.GetDetentionLetterReport();
                        List<BL.LettersType> lstReport = reportList.GetReportListForDownload(salesorderid);

                        _logger.LogInformation("{orderNumber} Send Letters: {letters}", _orderNumber, String.Join(",", lstReport));

                        DetentionLetterReport dlr = new DetentionLetterReport(_logger);

                        foreach (BL.LettersType detentionletter in lstReport)
                        {
                            //Insert History record according to report type
                            dlr.InsertOrderHistory(dlOrderSummary, detentionletter.ToString());
                        }

                        _logger.LogInformation("{orderNumber}: Insert History record according to report type", _orderNumber);

                        if (lstReport.Count > 0)
                        {
                            //Copy Detention letter on document path of order
                            dlr.GenerateReport(dlOrderSummary);
                            _logger.LogInformation("{orderNumber}: Copy Detention letter on document path of order", _orderNumber);

                            if (IsValidEmailAddress(dlOrderSummary.SoldToEmail))
                            {
                                try
                                {
                                    //Get SESell1 User and Add into CC Userlist
                                    lstUser.Add(dLtrrAct.GetSESell1Detail(dlOrderSummary.OrderNumber));
                                    //Copy Detention letter from document path and attach to email and send it
                                    bool isSendEmail = dlr.SendReportEmail(dlOrderSummary, lstUser);
                                    _logger.LogInformation("{orderNumber}: Copy Detention letter from document path and attach to email and send it", _orderNumber);

                                    if (isSendEmail)
                                    {
                                        //Update Send status and message
                                        dlr.UpdateStatus(dlOrderSummary, true, "Successful");
                                        _logger.LogInformation("{orderNumber}: Update Send status and Send Summary Report", _orderNumber);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Exception in processing order {orderNumber}", _orderNumber);
                                }
                            }
                            else
                            {
                                //Update Send status and message
                                dlr.UpdateStatus(dlOrderSummary, true, "SoldTo Email is not exist or Invalid for Sold to Contact.");
                                dlr.SendMissingDetailEmail(dlOrderSummary.OrderModifiedBy, dlOrderSummary.OrderNumber);
                                _logger.LogInformation("{orderNumber}: Update Status and Send EMail to order User.", _orderNumber);
                            }
                        }
                        else
                        {
                            //Update message qualified for send report or not
                            dlr.UpdateStatus(dlOrderSummary, true, "Not Qualified for Letter");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No pending detention letters found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in timer trigger function");
            }

            _logger.LogInformation("Timer trigger function completed at: {time}", DateTime.Now);
        }

        private bool IsValidEmailAddress(string address)
        {
            EmailAddressAttribute emailAttr = new EmailAddressAttribute();

            if (address != null && emailAttr.IsValid(address))
                return true;

            return false;
        }
    }

    public class TimerInfo
    {
        public TimerScheduleStatus? ScheduleStatus { get; set; }
        public bool IsPastDue { get; set; }
    }

    public class TimerScheduleStatus
    {
        public DateTime Last { get; set; }
        public DateTime Next { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
