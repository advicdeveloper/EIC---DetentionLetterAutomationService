using System;
using System.Collections.Generic;
using DA = CONTECH.Service.DataAccess;
using BE = CONTECH.Service.BusinessEntities;

namespace CONTECH.Service.BusinessLogic
{
    public class DetentionLetterHistoryAction
    {
        public bool InsertOrderHistory(BE.OrderSummary _ordersummary, string reportName)
        {
            bool isSave = false;
            DA.OrderHistoryRepositry queueDA = new DA.OrderHistoryRepositry();
            try
            {
                //string documentpath =
                string stFilePath = _ordersummary.DocumentPath;
                //A.If the order is Project - based, the document should be in the Order subfolder within the Project folder.
                if (_ordersummary.OpportunityId != Guid.Empty)
                {
                    stFilePath = stFilePath + "\\Order";
                }
                //B.If the Order is from a Transactional Quote, then it should be stored in the Quote folder, since there is no Order Subfolder at that level. 
                //C.If the Order is a direct order then it should be stored in the Order folder.
                //For B and C path is saved on order header

                string strFileName = reportName + "-" + _ordersummary.OrderNumber + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".pdf";
                string strfullpath = stFilePath + "\\" + strFileName;
                isSave = queueDA.InsertOrderHistory(_ordersummary, reportName, strfullpath, strFileName);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return isSave;
        }
        public bool UpdateHistoryReportStatus(Guid pHistoryId, bool pSucessfull, string message)
        {
            bool isUpdated = false;
            DA.OrderHistoryRepositry queueDA = new DA.OrderHistoryRepositry();
            try
            {
                isUpdated = queueDA.UpdateHistoryReportStatus(pHistoryId, pSucessfull, message);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return isUpdated;
        }
        public bool UpdateHistoryReportMessage(Guid pHistoryId, bool pReportgenerated, double fileSize, string message = "")
        {
            bool isUpdated = false;

            DA.OrderHistoryRepositry queueDA = new DA.OrderHistoryRepositry();
            try
            {
                if (pReportgenerated && string.IsNullOrEmpty(message))
                {
                    message = "Report Save Successful at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                isUpdated = queueDA.UpdateHistoryReportMessage(pHistoryId, pReportgenerated, fileSize, message);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return isUpdated;
        }
        public List<BE.OrderHistory> GetOrderHistory(Guid summaryId)
        {
            DA.OrderHistoryRepositry queueDA = new DA.OrderHistoryRepositry();
            List<BE.OrderHistory> historyList = new List<BE.OrderHistory>();
            try
            {
                historyList = queueDA.GetOrderHistory(summaryId);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return historyList;
        }
    }
}
