using CONTECH.Service.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BE = CONTECH.Service.BusinessEntities;

namespace CONTECH.Service.DataAccess
{
    public class DetentionLetterRepository : Repository
    {
        #region Summary Report methods

        public List<BE.OrderSummary> GetPendingDetentionLetter()
        {
            List<BE.OrderSummary> lstJobList = new List<BE.OrderSummary>();
            SqlDataReader reader = null;
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                //According to spec 602 only contech fulfiled order is exist in table.
                string spname = "GetPendingDetentionSummary";
                SqlCommand cmd = ConstructCommand(spname);
                reader = Find(cmd);
                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lstJobList.Add(Construct(reader));
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseReader(reader);
                CloseConnection();
            }
            return lstJobList;
        }

        public bool UpdateHeaderReportStatus(Guid pSummaryId, bool pSucessfull, string message)
        {
            bool isUpdated = false;
            SqlCommand cmd;
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                cmd = ConstructQueryCommand(@"UPDATE DetentionOrderSummary SET SendDate=GETDATE(), IsSend=1,Message='" + message + "' WHERE SummaryId='" + pSummaryId.ToString() + "'");
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

        public BE.Users GetUserDetail(Guid pUserid)
        {
            BE.Users lstJobList = new BE.Users();
            SqlDataReader reader = null;
            try
            {
                OpenConnection(ConnectTo.CRM);
                //According to spec 602 user email.
                string sql = @"Select * from SystemUserBase with(nolock) where SystemUserId='" + pUserid.ToString() + "'";
                SqlCommand cmd = ConstructQueryCommand(sql);
                reader = Find(cmd);
                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lstJobList = ConstructUser(reader);
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseReader(reader);
                CloseConnection();
            }
            return lstJobList;
        }
        /// <summary>
        /// Get SeSell1 User Details based on Order to Send mail
        /// </summary>
        /// <param name="orderNumber">orderNumber</param>
        /// <returns>Users</returns>
        public Users GetSESell1Detail(string orderNumber)
        {
            BE.Users lstJobList = new BE.Users();
            SqlDataReader reader = null;
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                //According to spec 602.2 SESEll user email. 
                string spname = "GetUserDetailForDetentionLetter";
                SqlCommand cmd = ConstructCommand(spname);
                cmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                reader = Find(cmd);
                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lstJobList = ConstructUser(reader);
                    }
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                CloseReader(reader);
                CloseConnection();
            }
            return lstJobList;
        }

        public Users ConstructUser(SqlDataReader reader)
        {
            BE.Users nJob = new BE.Users
            {
                UserId = GetFieldValue(reader, "SystemUserId", Guid.Empty),
                UserName = GetFieldValue(reader, "FullName", string.Empty),
                FullName = GetFieldValue(reader, "DomainName", string.Empty),
                Email = GetFieldValue(reader, "InternalEMailAddress", string.Empty),
                Title = GetFieldValue(reader, "Title", string.Empty)
            };

            return nJob;
        }


        #endregion

        public BE.OrderSummary Construct(SqlDataReader reader)
        {
            BE.OrderSummary nJob = new BE.OrderSummary
            {
                SummaryId = GetFieldValue(reader, "SummaryId", Guid.Empty),//  pReader.GetGuid(pReader.GetOrdinal("")),
                OrderId = GetFieldValue(reader, "OrderId", Guid.Empty),
                OrderDate = GetFieldValue(reader, "OrderDate", DateTime.MinValue),
                OrderNumber = GetFieldValue(reader, "OrderNumber", string.Empty),
                OpportunityId = GetFieldValue(reader, "OpportunityId", Guid.Empty),
                ProjectName = GetFieldValue(reader, "ProjectName", string.Empty),
                ProjectNumber = GetFieldValue(reader, "ProjectNumber", string.Empty),
                QuoteId = GetFieldValue(reader, "QuoteId", Guid.Empty),
                QuoteName = GetFieldValue(reader, "QuoteName", string.Empty),
                QuoteNumber = GetFieldValue(reader, "QuoteNumber", string.Empty),
                SoldToContactid = GetFieldValue(reader, "SoldToContactid", Guid.Empty),
                SoldToContactName = GetFieldValue(reader, "", string.Empty),
                SoldToPhone = GetFieldValue(reader, "SoldToPhone", string.Empty),
                SoldToEmail = GetFieldValue(reader, "SoldToEmail", string.Empty),
                DocumentPath = GetFieldValue(reader, "DocumentPath", string.Empty),
                OwningBusinessUnit = GetFieldValue(reader, "OwningBusinessUnit", Guid.Empty),
                CustomerId = GetFieldValue(reader, "CustomerId", Guid.Empty),
                CustomerName = GetFieldValue(reader, "CustomerName", string.Empty),
                IsSend = GetFieldValue(reader, "IsSend", false),
                OwnerId = GetFieldValue(reader, "OwnerId", Guid.Empty),
                OrderModifiedBy = GetFieldValue(reader, "OrderModifiedBy", Guid.Empty),
                CreatedBy = GetFieldValue(reader, "CreatedBy", Guid.Empty),
                CreatedOn = GetFieldValue(reader, "CreatedOn", DateTime.MinValue),
                SendDate = GetFieldValue(reader, "SendDate", DateTime.MinValue),
                OrderName = GetFieldValue(reader, "OrderName", string.Empty),
                Message = GetFieldValue(reader, "Message", string.Empty),
                City = GetFieldValue(reader, "City", string.Empty),
                State = GetFieldValue(reader, "State", string.Empty)
            };

            return nJob;
        }
    }
}
