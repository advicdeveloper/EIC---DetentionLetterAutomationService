using System;
using System.Collections.Generic;
using DA = CONTECH.Service.DataAccess;
using BE = CONTECH.Service.BusinessEntities;


namespace CONTECH.Service.BusinessLogic
{
    public class DetentionLetterSummaryAction
    {
        #region Summary Report methods

        public List<BE.OrderSummary> GetPendingDetentionLetter()
        {
            List<BE.OrderSummary> summaryIdLst = new List<BE.OrderSummary>();
            DA.DetentionLetterRepository queueDA = new DA.DetentionLetterRepository();

            try
            {
                summaryIdLst = queueDA.GetPendingDetentionLetter();
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return summaryIdLst;
        }

        public bool UpdateHeaderReportStatus(Guid pSummaryId, bool pSucessfull, string message)
        {
            bool isUpdated = false;

            DA.DetentionLetterRepository queueDA = new DA.DetentionLetterRepository();
            try
            {
                isUpdated = queueDA.UpdateHeaderReportStatus(pSummaryId, pSucessfull, message);
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

        public BE.Users GetUserDetail(Guid pUserid)
        {
            BE.Users userid = new BE.Users();
            DA.DetentionLetterRepository queueDA = new DA.DetentionLetterRepository();

            try
            {
                userid = queueDA.GetUserDetail(pUserid);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return userid;
        }

        /// <summary>
        /// Get SeSell1 User Details based on Order to Send mail
        /// </summary>
        /// <param name="orderNumber">orderNumber</param>
        /// <returns>User</returns>
        public BE.Users GetSESell1Detail(string orderNumber) 
        {
            BE.Users userid = new BE.Users();
            DA.DetentionLetterRepository queueDA = new DA.DetentionLetterRepository();

            try
            {
                userid = queueDA.GetSESell1Detail(orderNumber);
            }
            catch
            {
                throw;
            }
            finally
            {
                queueDA = null;
            }
            return userid;
        }
        #endregion
    }
}
