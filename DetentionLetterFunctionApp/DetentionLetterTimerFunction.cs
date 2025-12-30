using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BE = CONTECH.Service.BusinessEntities;
using BL = CONTECH.Service.BusinessLogic;

namespace DetentionLetterFunctionApp
{
    public class DetentionLetterTimerFunction
    {
        private readonly IConfiguration _configuration;
        private readonly DetentionLetterProcessor _processor;

        public DetentionLetterTimerFunction(IConfiguration configuration)
        {
            _configuration = configuration;
            _processor = new DetentionLetterProcessor(configuration);
        }

        [FunctionName("DetentionLetterTimerFunction")]
        public void Run([TimerTrigger("%TimerSchedule%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Detention Letter Timer Function executed at: {DateTime.Now}");
            Logger.Info("Function started successfully");

            try
            {
                BL.DetentionLetterSummaryAction dLtrrAct = new BL.DetentionLetterSummaryAction();
                List<BE.OrderSummary> lstHistoryIds = dLtrrAct.GetPendingDetentionLetter();

                log.LogInformation($"Found {lstHistoryIds.Count} pending detention letters to process");
                Logger.Info($"Found {lstHistoryIds.Count} pending detention letters to process");

                if (lstHistoryIds.Count > 0)
                {
                    foreach (BE.OrderSummary dlOrderSummary in lstHistoryIds)
                    {
                        ProcessOrder(dlOrderSummary, dLtrrAct, log);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception in timer function: {ex.Message}");
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in timer function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }

            log.LogInformation("Function execution completed");
            Logger.Info("Function execution completed");
        }

        private void ProcessOrder(BE.OrderSummary dlOrderSummary, BL.DetentionLetterSummaryAction dLtrrAct, ILogger log)
        {
            try
            {
                List<BE.Users> lstUser = new List<BE.Users>();
                //Get Modified User and Add into CC UserList
                lstUser.Add(dLtrrAct.GetUserDetail(dlOrderSummary.OrderModifiedBy));
                string salesorderid = Convert.ToString(dlOrderSummary.OrderId);
                string _orderNumber = dlOrderSummary.OrderNumber;

                log.LogInformation($"Processing order: {_orderNumber}");
                Logger.Info($"Processing order: {_orderNumber}");

                //Get Detention Report list using spec  #602 condition
                BL.GetDetentionLetterReport reportList = new BL.GetDetentionLetterReport();
                List<BL.LettersType> lstReport = reportList.GetReportListForDownload(salesorderid);

                Logger.Info(_orderNumber + " Send Letters" + String.Join(",", lstReport));

                foreach (BL.LettersType detentionletter in lstReport)
                {
                    //Insert History record according to report type
                    _processor.InsertOrderHistory(dlOrderSummary, detentionletter.ToString());
                }
                Logger.Info(_orderNumber + ": Insert History record according to report type");

                if (lstReport.Count > 0)
                {
                    //Copy Detention letter on document path of order
                    _processor.GenerateReport(dlOrderSummary);
                    Logger.Info(_orderNumber + ": Copy Detention letter on document path of order");

                    if (IsValidEmailAddress(dlOrderSummary.SoldToEmail))
                    {
                        try
                        {
                            //Get SESell1 User and Add into CC Userlist
                            lstUser.Add(dLtrrAct.GetSESell1Detail(dlOrderSummary.OrderNumber));
                            //Copy Detention letter from document path and attach to email and send it
                            bool isSendEmail = _processor.SendReportEmail(dlOrderSummary, lstUser);
                            Logger.Info(_orderNumber + ": Copy Detention letter from document path and attach to email and send it");
                            if (isSendEmail)
                            {
                                //Update Send status and message
                                _processor.UpdateStatus(dlOrderSummary, true, "Successful");
                                Logger.Info(_orderNumber + ": Update Send status");
                                log.LogInformation($"Successfully processed and sent email for order: {_orderNumber}");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogError($"Error processing order {_orderNumber}: {ex.Message}");
                            Logger.Error(DateTime.Today.ToLongDateString() + " Exception processing order " + _orderNumber + " " + ex.StackTrace);
                            Logger.Error("Message ------ " + ex.Message);
                        }
                    }
                    else
                    {
                        //Update Send status and message
                        _processor.UpdateStatus(dlOrderSummary, true, "SoldTo Email is not exist or Invalid for Sold to Contact.");
                        _processor.SendMissingDetailEmail(dlOrderSummary.OrderModifiedBy, dlOrderSummary.OrderNumber);
                        Logger.Info(_orderNumber + ": Update Status and Send EMail to order User.");
                        log.LogWarning($"Invalid email for order {_orderNumber}");
                    }
                }
                else
                {
                    //Update message qualified for send report or not
                    _processor.UpdateStatus(dlOrderSummary, true, "Not Qualified for Letter");
                    log.LogInformation($"Order {_orderNumber} not qualified for letter");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception processing order: {ex.Message}");
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in ProcessOrder " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
        }

        private bool IsValidEmailAddress(string address)
        {
            EmailAddressAttribute emailAttr = new EmailAddressAttribute();

            if (address != null && emailAttr.IsValid(address))
                return true;

            return false;
        }
    }
}
