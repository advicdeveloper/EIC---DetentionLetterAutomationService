using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceProcess;
using System.Timers;
using BL = CONTECH.Service.BusinessLogic;
using BE = CONTECH.Service.BusinessEntities;
using System.ComponentModel.DataAnnotations;

namespace DetentionLetterAutomationService
{
    partial class DetentionLetterAutomationService : ServiceBase
    {
        private Timer _aTimer = null;
        public DetentionLetterAutomationService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            _aTimer = new Timer();
            Logger.Info("Service started successfully");
            _aTimer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["TimerValue"]); //30 * 60 * 1000 120000
            _aTimer.Elapsed += new System.Timers.ElapsedEventHandler(_aTimer_Elapsed);
            _aTimer.Enabled = true;
            Logger.Info("Service Initialized successfully");
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            _aTimer.Enabled = false;
            Logger.Info("Service stopped successfully");
        }


        public void Start()
        {
            OnStart(null);
        }

        void _aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Info("Timer  elapsed call successfully");
            try
            {
                _aTimer.Enabled = false;
                _aTimer.Stop(); // stop timer 
                BL.DetentionLetterSummaryAction dLtrrAct = new BL.DetentionLetterSummaryAction();
                List<BE.OrderSummary> lstHistoryIds = dLtrrAct.GetPendingDetentionLetter();

                if (lstHistoryIds.Count > 0)
                {
                    foreach (BE.OrderSummary dlOrderSummary in lstHistoryIds)
                    {
                        List<BE.Users> lstUser = new List<BE.Users>();
                        //Get Modified User and Add into CC UserList
                        lstUser.Add(dLtrrAct.GetUserDetail(dlOrderSummary.OrderModifiedBy));
                        string salesorderid = Convert.ToString(dlOrderSummary.OrderId);
                        string _orderNumber = dlOrderSummary.OrderNumber;
                        //Get Detention Report list using spec  #602 condition
                        BL.GetDetentionLetterReport reportList = new BL.GetDetentionLetterReport();
                        List<BL.LettersType> lstReport = reportList.GetReportListForDownload(salesorderid);

                        Logger.Info(_orderNumber + " Send Letters" + String.Join(",", lstReport));
                        DetentionLetterReport dlr = new DetentionLetterReport();

                        foreach (BL.LettersType detentionletter in lstReport)
                        {
                            //Insert History record according to report type
                            dlr.InsertOrderHistory(dlOrderSummary, detentionletter.ToString());
                        }
                        Logger.Info(_orderNumber + ": Insert History record according to report type");
                        if (lstReport.Count > 0)
                        {
                            //Copy Detention letter on document path of order
                            dlr.GenerateReport(dlOrderSummary);
                            Logger.Info(_orderNumber + ":Copy Detention letter on document path of order");

                            if (IsValidEmailAddress(dlOrderSummary.SoldToEmail))
                            {
                                try
                                {
                                    //Get SESell1 User and Add into CC Userlist
                                    lstUser.Add(dLtrrAct.GetSESell1Detail(dlOrderSummary.OrderNumber));
                                    //Copy Detention letter from document path and attach to email and send it
                                    bool isSendEmail = dlr.SendReportEmail(dlOrderSummary, lstUser);
                                    Logger.Info(_orderNumber + ":Copy Detention letter from document path and attach to email and send it");
                                    if (isSendEmail)
                                    {
                                        //Update Send status and message
                                        dlr.UpdateStatus(dlOrderSummary, true, "Successful");
                                        //As discuss with joanna not send summry report to user we cc user in main email
                                        //dlr.SendSummaryReportEmail(dlOrderSummary);
                                        Logger.Info(_orderNumber + ":Update Send status and Send Summary Report");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(DateTime.Today.ToLongDateString() + " Exception in _aTimer_Elapsed timer elapsed event " + ex.StackTrace);
                                    Logger.Error("Message ------ " + ex.Message);
                                }
                            }
                            else
                            {
                                //Update Send status and message
                                dlr.UpdateStatus(dlOrderSummary, true, "SoldTo Email is not exist or Invalid for Sold to Contact.");
                                dlr.SendMissingDetailEmail(dlOrderSummary.OrderModifiedBy, dlOrderSummary.OrderNumber);
                                Logger.Info(_orderNumber + ":Update Status and Send EMail to order User.");
                            }
                        }
                        else
                        {
                            //Update message qulified for send report or not
                            dlr.UpdateStatus(dlOrderSummary, true, "Not Qualified for Letter");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in _aTimer_Elapsed timer elapsed event " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
            finally
            {
                _aTimer.Enabled = true;
                _aTimer.Start();
                Logger.Info("Timer  elapsed end successfully");
            }

        }
        public bool IsValidEmailAddress(string address)
        {
            EmailAddressAttribute emailAttr = new EmailAddressAttribute();

            if (address != null && emailAttr.IsValid(address))
                return true;

            return false;
        }
    }
}
