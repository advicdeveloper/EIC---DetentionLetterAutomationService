using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using BE = CONTECH.Service.BusinessEntities;

namespace CONTECH.Service.DataAccess
{
    public class OrderHistoryRepositry : Repository
    {
        public bool InsertOrderHistory(BE.OrderSummary _ordersummary, string reportName, string filepath, string fileName)
        {

            bool IsAdded = false;
            OpenConnection(ConnectTo.CustomCRMDB);
            try
            {
                SqlCommand cmd = ConstructCommand("InsertDetentionOrderHistory");
                AddInParameter(cmd, "@SummaryId", DbType.Guid, _ordersummary.SummaryId);
                AddInParameter(cmd, "@AssociatedToOrderId", DbType.Guid, _ordersummary.OrderId);
                AddInParameter(cmd, "@OrderNumber", DbType.String, _ordersummary.OrderNumber);
                AddInParameter(cmd, "@CreatedBy", DbType.Guid, _ordersummary.OrderModifiedBy);
                AddInParameter(cmd, "@ReportName", DbType.String, reportName);
                AddInParameter(cmd, "@FromAddress", DbType.String, ConfigurationManager.AppSettings["EmailFrom"]);
                AddInParameter(cmd, "@SentToAddress", DbType.String, _ordersummary.SoldToEmail);
                AddInParameter(cmd, "@OwningBusinessUnit", DbType.Guid, _ordersummary.OwningBusinessUnit);
                AddInParameter(cmd, "@AttachmentPath", DbType.String, filepath);
                AddInParameter(cmd, "@FileName", DbType.String, fileName);
                IsAdded = Add(cmd);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return IsAdded;
        }

        public List<BE.OrderHistory> GetOrderHistory(Guid summaryId)
        {

            OpenConnection(ConnectTo.CustomCRMDB);
            string sql = @"Select * from DetentionOrderHistory with(nolock) where SummaryId='" + summaryId + "'";
            SqlCommand cmd = ConstructQueryCommand(sql);
            SqlDataReader reader = Find(cmd);
            List<BE.OrderHistory> details = new List<BE.OrderHistory>();
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    details.Add(Construct(reader));
                }
            }
            CloseReader(reader);
            return details;
        }

        public BE.OrderHistory Construct(SqlDataReader reader)
        {
            BE.OrderHistory _History = new BE.OrderHistory
            {
                HistoryId = GetFieldValue(reader, "HistoryId", Guid.Empty),
                SummaryId = GetFieldValue(reader, "SummaryId", Guid.Empty),
                AssociatedToOrderId = GetFieldValue(reader, "AssociatedToOrderId", Guid.Empty),
                OrderNumber = GetFieldValue(reader, "OrderNumber", string.Empty),
                OwningBusinessUnit = GetFieldValue(reader, "OwningBusinessUnit", Guid.Empty),
                ErrorMessage = GetFieldValue(reader, "ErrorMessage", string.Empty),
                ReportName = GetFieldValue(reader, "ReportName", string.Empty),
                SendDate = GetFieldValue(reader, "SendDate", DateTime.MinValue),
                IsSend = GetFieldValue(reader, "IsSend", false),
                CreatedBy = GetFieldValue(reader, "CreatedBy", Guid.Empty),
                CreatedOn = GetFieldValue(reader, "CreatedOn", DateTime.MinValue),
                FromAddress = GetFieldValue(reader, "FromAddress", string.Empty),
                SentToAddress = GetFieldValue(reader, "SentToAddress", string.Empty),
                AttachmentPath = GetFieldValue(reader, "AttachmentPath", string.Empty),
                Size = GetFieldValue(reader, "Size", 0),
                FileName = GetFieldValue(reader, "FileName", string.Empty)
            };
            return _History;
        }

        public bool UpdateHistoryReportMessage(Guid pHistoryId, bool pReportgenerated, double fileSize, string message)
        {
            bool isUpdated = false;
            SqlCommand cmd;
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                cmd = ConstructQueryCommand(@"UPDATE DetentionOrderHistory SET Size =" + fileSize + " ,ErrorMessage='" + message + "' WHERE HistoryId='" + pHistoryId.ToString() + "'");
                isUpdated = Update(cmd);
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseConnection();
            }
            return isUpdated;
        }

        public bool UpdateHistoryReportStatus(Guid pHistoryId, bool pSucessfull, string message)
        {
            bool isUpdated = false;
            SqlCommand cmd;
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                cmd = ConstructQueryCommand(@"UPDATE DetentionOrderHistory SET IsSend=" + (pSucessfull ? 1 : 0) + ",SendDate=GETDATE(), ErrorMessage='" + message + "' WHERE HistoryId='" + pHistoryId.ToString() + "'");
                isUpdated = Update(cmd);
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseConnection();
            }
            return isUpdated;
        }
    }
}
