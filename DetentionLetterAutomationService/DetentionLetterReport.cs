using Contech.Utilities;
using CONTECH.Service.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using BE = CONTECH.Service.BusinessEntities;

using BL = CONTECH.Service.BusinessLogic;

namespace DetentionLetterAutomationService
{
    public class DetentionLetterReport
    {
        public void GenerateReport(BE.OrderSummary dlOrderSummary)
        {
            string rootpath = dlOrderSummary.DocumentPath;
            BL.DetentionLetterHistoryAction dLtrrHistoryAct = new BL.DetentionLetterHistoryAction();
            try
            {
                List<BE.OrderHistory> lstHistory = dLtrrHistoryAct.GetOrderHistory(dlOrderSummary.SummaryId);
                foreach (BE.OrderHistory dlOrderHistory in lstHistory)
                {
                    //Generate file
                    //byte[] result = GenerateReport(dlOrderHistory.AssociatedToOrderId, dlOrderHistory.ReportName, dlOrderSummary.OrderModifiedBy);
                    byte[] result = DownLoadFileByWebRequest(dlOrderHistory.AssociatedToOrderId, dlOrderHistory.ReportName, dlOrderSummary.OrderModifiedBy);

                    //////file save on documentpath of order
                    bool isfilesave = UploadReport(result, rootpath, dlOrderHistory.AttachmentPath);

                    if (isfilesave)
                    {
                        //Size saved in bytes
                        double filelength = new FileInfo(dlOrderHistory.AttachmentPath).Length;

                        //Update history log
                        dLtrrHistoryAct.UpdateHistoryReportMessage(dlOrderHistory.HistoryId, true, filelength);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in GenerateReport function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
        }

        private bool UploadReport(byte[] filedata, string dirPAth, string fPath)
        {
            try
            {
                if (Directory.Exists(dirPAth))
                {
                    if (File.Exists(fPath))
                    {
                        File.Delete(fPath);
                    }

                    using (FileStream fs = new FileStream(fPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        //Create StreamWriter object to write string to FileSream
                        StreamWriter sw = new StreamWriter(fs);
                        //StreamWriter for writing bytestream array to file document
                        sw.BaseStream.Write(filedata, 0, filedata.Length);
                        sw.Flush();
                        sw.Close();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in UploadReport function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
            return false;
        }

        public bool SendReportEmail(BE.OrderSummary summary, List<BE.Users> lstUserCC)
        {
            bool isSend = false;

            try
            {
                string emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
                string emailTo = summary.SoldToEmail;
                string emailSubject = "Important Installation Procedures: " + summary.OrderNumber + ": " + summary.OrderName + " – " + summary.City + ", " + summary.State;
                string emailCC = string.Join(";", lstUserCC.Where(a => a.Email != null).Select(a => a.Email).Distinct());
                string emailBody = "Please find attached critical documents that provide the proper procedures for installation and backfilling of material your company recently ordered from Contech.    Please contact your local sales engineer if you have specific questions related to the installation process.";

                List<Attachment> emailAttachment = new List<Attachment>();

                #region Get Detention Letter Attachemnt

                BL.DetentionLetterHistoryAction dLtrrHistoryAct = new BL.DetentionLetterHistoryAction();
                List<BE.OrderHistory> lstHistory = dLtrrHistoryAct.GetOrderHistory(summary.SummaryId);

                string appPath = AppDomain.CurrentDomain.BaseDirectory;

                foreach (BE.OrderHistory hrec in lstHistory)
                {
                    //Attach Lg Diameter latter in email for doc path
                    if (!string.IsNullOrEmpty(hrec.AttachmentPath))
                    {
                        Attachment messagetransmittalAttachment = new Attachment(hrec.AttachmentPath);
                        emailAttachment.Add(messagetransmittalAttachment);
                    }

                    #region Attach Extra documents 
                    //1.CMP Large Diameter Letter   NCSPA Installation Manual for CSP, Pipe Arches & Structural Plate
                    if (hrec.ReportName == BL.LettersType.CMPLargeDiameterLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\NCSPA Installation Manual for CSP, Pipe Arches and Structural Plate.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    //2. CMP Detention Letter    CMP Detention Installation Guide
                    else if (hrec.ReportName == BL.LettersType.CMPDetentionLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\CMP Detention Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    //3. DuroMaxx Cistern RWH Letter Installation Guide – DuroMaxx SRPE - Tank
                    else if (hrec.ReportName == BL.LettersType.DuroMaxxCisternRWHLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\DuroMaxx SRPE-Tank Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    //4. DuroMaxx Containment Letter	Installation Guide – DuroMaxx SRPE-Tank
                    else if (hrec.ReportName == BL.LettersType.DuroMaxxContainmentTankNotificationLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\DuroMaxx SRPE-Tank Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    //5.DuroMaxx Lg Diameter Letter DMX Installation Guide
                    else if (hrec.ReportName == BL.LettersType.DuroMaxxLgDiameterLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\DMX Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    //6.DuroMaxx Detention Letter   DMX Detention Installation Guide - color Nov 2020 and CMP Detention Installation Guide
                    else if (hrec.ReportName == BL.LettersType.DuroMaxxDetentionLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\DMX Detention Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);

                        Attachment messagetransmittalAttachment1 = new Attachment(appPath + "\\LetterDocuments\\CMP Detention Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment1);

                    }
                    //7. DuroMaxx Sewer Letter	Installation Guide – DuroMaxx SRPE-Tank
                    else if (hrec.ReportName == BL.LettersType.DuroMaxxSewerLetter.ToString())
                    {
                        Attachment messagetransmittalAttachment = new Attachment(appPath + "\\LetterDocuments\\DuroMaxx SRPE-Tank Installation Guide.pdf");
                        emailAttachment.Add(messagetransmittalAttachment);
                    }
                    // Attachment messagetransmittalAttachment = new Attachment(fileattachStream, Path.GetFileName(hrec.AttachmentPath));
                    #endregion
                }
                #endregion Get Detention Letter Attachemnt

                isSend = SendEmail(emailFrom, emailTo, emailCC, emailSubject, emailBody, emailAttachment);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isSend;
        }

        public void SendMissingDetailEmail(Guid modifiedby, string ordernumber)
        {
            try
            {
                List<Attachment> emailAttachment = new List<Attachment>();

                BL.DetentionLetterSummaryAction dLtrrAct = new BL.DetentionLetterSummaryAction();
                BE.Users ordermodiedby = dLtrrAct.GetUserDetail(modifiedby);

                //if no recipient or sender then do not attempt mail sending.
                if (string.IsNullOrEmpty(ordermodiedby.Email))
                {
                    Logger.Error(DateTime.Today.ToLongDateString() + " SendMissingDetailEmail->SendEmail: No Mail Recipient/Sender - Subject " + ordermodiedby.FullName);
                    return;
                }

                string emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
                string emailTo = ordermodiedby.Email;
                string emailSubject = "ALERT:  Large Diameter/Detention Letter was NOT sent for: " + ordernumber;
                string emailBody = "The Sold To Contact associated to this Order does not have an email address.  You must manually create and send the appropriate letter and attachment to the customer.  Please update the contact’s email address in CRM so it is available for the next order.";
                SendEmail(emailFrom, emailTo, "", emailSubject, emailBody, emailAttachment);
                Logger.Info(emailSubject + " sent mail to Modified USER[SendMissingDetailEmail]-" + emailTo);
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in SendMissingDetailEmail->SendEmail Subject " + ordernumber + " " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
                throw ex;
            }
        }

        private bool SendEmail(string SentFrom, string SentTo, string SentCC, string Subject, string SentBody, List<Attachment> SentAttachments)
        {
            bool isSend = false;
            MailMessage msgMail = new MailMessage();
            SmtpClient mailClient = new SmtpClient(ConfigurationManager.AppSettings["EmailHost"], Convert.ToInt32(ConfigurationManager.AppSettings["EmailHostPort"])); //outlook smtp  

            try
            {
                string emailFrom = SentFrom;
                string emailTo = SentTo;
                string emailCC = SentCC;


                msgMail.From = new MailAddress(emailFrom);
                msgMail.To.Add(emailTo);
                msgMail.Subject = Subject;
                if (!string.IsNullOrEmpty(emailCC))
                {
                    foreach (var item in emailCC.Split(';'))
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            msgMail.CC.Add(item);
                        }
                    }                    
                }

                MailPriority priority = MailPriority.High;
                NetworkCredential basicCredential1 = GetDecryptedCredential(ConfigurationManager.AppSettings["EmailSender"], ConfigurationManager.AppSettings["EmailSenderPwd"]);
                //mailClient.EnableSsl = true;
                mailClient.UseDefaultCredentials = false;
                mailClient.Credentials = basicCredential1;
                msgMail.IsBodyHtml = true;
                msgMail.Body = SentBody;
                msgMail.Priority = priority;


                foreach (Attachment attachment in SentAttachments)
                {
                    msgMail.Attachments.Add(attachment);
                }
                mailClient.Send(msgMail);
                isSend = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //Clean up attachments
                foreach (Attachment attachment in msgMail.Attachments)
                {
                    attachment.Dispose();
                }
                msgMail.Dispose();
                mailClient.Dispose();
            }
            return isSend;

        }

        public void UpdateStatus(OrderSummary summary, bool pSucessfull, string message)
        {
            try
            {
                BL.DetentionLetterHistoryAction dLtrrHistoryAct = new BL.DetentionLetterHistoryAction();
                List<BE.OrderHistory> lstHistory = dLtrrHistoryAct.GetOrderHistory(summary.SummaryId);
                foreach (BE.OrderHistory hrec in lstHistory)
                {
                    dLtrrHistoryAct.UpdateHistoryReportStatus(hrec.HistoryId, pSucessfull, message);
                }
                BL.DetentionLetterSummaryAction dLsummaryAct = new BL.DetentionLetterSummaryAction();
                dLsummaryAct.UpdateHeaderReportStatus(summary.SummaryId, true, message);
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in UpdateStatus function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
        }

        public bool InsertOrderHistory(BE.OrderSummary _ordersummary, string reportName)
        {
            try
            {
                BL.DetentionLetterHistoryAction detentionLtrrHistory = new BL.DetentionLetterHistoryAction();
                return detentionLtrrHistory.InsertOrderHistory(_ordersummary, reportName);
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in InsertOrderHistory function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
            }
            return false;
        }

        private byte[] DownLoadFileByWebRequest(Guid pOrderId, string reportName, Guid pUserId)
        {
            try
            {
                string crmUrl = ConfigurationManager.AppSettings["ReportWebURL"];
                string urlAddress = crmUrl + "?salesOrderId=" + pOrderId.ToString() + "&reportname=" + reportName + "&User=" + pUserId.ToString();
                System.Net.HttpWebRequest request = null;
                System.Net.HttpWebResponse response = null;
                request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(urlAddress);
                request.Timeout = 30000;  //8000 Not work
                response = (System.Net.HttpWebResponse)request.GetResponse();
                Stream s = response.GetResponseStream();
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    s.Close();
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(DateTime.Today.ToLongDateString() + " Exception in DownLoadFileByWebRequest ('" + pOrderId + "') and  ('" + reportName + "') function " + ex.StackTrace);
                Logger.Error("Message ------ " + ex.Message);
                return null;
            }
        }

        private NetworkCredential GetDecryptedCredential(string username, string password)
        {
            NetworkCredential decryptedvalue;
            try
            {
                string _UserName = EncryptDecrypt.Decrypt(username);
                string _Password = EncryptDecrypt.Decrypt(password);


                decryptedvalue = new NetworkCredential(_UserName, _Password);
            }
            catch (Exception ex)
            {
                decryptedvalue = new NetworkCredential(username, password);
            }
            return decryptedvalue;
        }
    }
}