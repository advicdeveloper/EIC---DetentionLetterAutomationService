using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONTECH.Service.DataAccess
{
    public enum ConnectTo
    {
        CRM,
        CustomCRMDB,
        Datawarehouse,
        GreatPlains,
        GreatPlainsSW,
        GreatPlainsBR,
        CRMAttachmentArchive,
        CreditSuitDB,
        ContechWarehouse,
    }
    public class Repository
    {
        #region properties

        private string _ConnString;
        private string _UniqueOrganizationName;

        public SqlConnection Connection { get; set; }
        public SqlTransaction Transaction { get; set; }

        #endregion

        #region Constructor

        public Repository()
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsTestMode"].ToString()) == true)
                _ConnString = ConfigurationManager.ConnectionStrings["DevelopmentCustomConnectionString"].ToString();
            else
                _ConnString = ConfigurationManager.ConnectionStrings["ProductionCustomConnectionString"].ToString();
        }

        public Repository(String UniqueOrganizationName)
        {
            _UniqueOrganizationName = UniqueOrganizationName;

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsTestMode"].ToString()) == true)
                _ConnString = ConfigurationManager.ConnectionStrings["DevelopmentCustomConnectionString"].ToString();
            else
                _ConnString = ConfigurationManager.ConnectionStrings["ProductionCustomConnectionString"].ToString();
        }

        public Repository(SqlConnection pConnection)
        {
            this.Connection = pConnection;
        }

        public Repository(SqlConnection pConnection, SqlTransaction pTransaction)
        {
            this.Connection = pConnection;
            this.Transaction = pTransaction;
        }

        #endregion

        public SqlCommand ConstructCommand(String pStoredProcedure)
        {
            SqlCommand cmd = new SqlCommand();

            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.Connection = Connection;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = pStoredProcedure;

            //if (pStoredProcedure != "ValidateUser" && pStoredProcedure != "ValidateDealer" && pStoredProcedure != "ValidateOperator")
            //AddInParameter(cmd, "@profileid", DbType.Guid, Guid.Empty);
            return cmd;
        }

        public SqlCommand ConstructQueryCommand(String pQuery)
        {
            SqlCommand cmd = new SqlCommand();

            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.Connection = Connection;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = pQuery;
            return cmd;
        }

        public bool Add(SqlCommand pCommand)
        {
            bool result = false;
            try
            {
                int n = pCommand.ExecuteNonQuery();
                result = true;
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
                CloseConnection();
            }
            return result;
        }

        public bool Update(SqlCommand pCommand)
        {
            bool result = false;
            try
            {
                pCommand.ExecuteNonQuery();
                result = true;
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
                CloseConnection();
            }
            return result;
        }

        public bool Delete(SqlCommand pCommand)
        {
            bool result = false;
            try
            {
                pCommand.ExecuteNonQuery();
                result = true;
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
                this.CloseConnection();
            }
            return result;
        }

        public SqlDataReader Find(SqlCommand pCommand)
        {
            SqlDataReader result = null;
            try
            {
                result = pCommand.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
                this.CloseConnection();
            }
            return result;
        }

        public void OpenConnection()
        {
            if (Connection == null)
            {
                Connection = new SqlConnection(_ConnString);
            }
            Connection.Open();
        }

        public void OpenConnection(ConnectTo pConnectDB)
        {
            try
            {
                if (Connection != null)
                    CloseConnection();

                SetConnectionString(pConnectDB);
                Connection = new SqlConnection(_ConnString);

                Connection.Open();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void CloseConnection()
        {
            if (Connection != null)
            {
                try
                {
                    if (Connection.State == ConnectionState.Open)
                    {
                        Connection.Close();
                    }
                    Connection.Dispose();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void CloseReader(SqlDataReader reader)
        {
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
        }

        public void BeginTransaction()
        {
            Transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (Transaction == null || Connection == null)
                throw new InvalidOperationException("Transaction and connection must not be null when calling Commit");

            Transaction.Commit();
            Connection.Close();
            Connection.Dispose();
        }

        public void RollbackTransaction()
        {
            if (Transaction == null || Connection == null)
                throw new InvalidOperationException("Transaction and connection must not be null when calling RollBack");

            Transaction.Rollback();
            Connection.Close();
            Connection.Dispose();
        }

        private void HandleSqlException(SqlException ex)
        {
            switch (ex.Number)
            {
                case ErrorCodes.ValidationError:
                    throw new Exception("", ex);
                case ErrorCodes.ConcurrencyViolationError:
                    throw new Exception("", ex);
                case ErrorCodes.UniqueKeyViolationError:
                    throw new Exception("A record with this value already exists. Please enter different value to avoid duplicate values.", ex);
                default:
                    throw ex;
            }
        }
        protected void LogException(Exception ex)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["ThrowErrors"] == "1")
                throw ex;
        }

        public void AddInParameter(DbCommand command, String name, DbType dbType, Object value)
        {
            SqlParameter prm = new SqlParameter();
            prm.DbType = dbType;
            prm.Value = value;
            prm.Direction = ParameterDirection.Input;
            prm.ParameterName = name;
            command.Parameters.Add(prm);
        }

        public void AddOutParameter(DbCommand command, String name, DbType dbType, Object value)
        {
            SqlParameter prm = new SqlParameter();
            prm.DbType = dbType;
            prm.Value = value;
            prm.Direction = ParameterDirection.Output;
            prm.ParameterName = name;
            if (dbType == DbType.String)
                prm.Size = 100;
            command.Parameters.Add(prm);
        }

        public static T GetFieldValue<T>(SqlDataReader reader, string fieldName, T defaultvalue = default(T))
        {
            try
            {
                var value = reader[fieldName];
                if (value == DBNull.Value || value == null)
                    return defaultvalue;
                return (T)value;
            }
            catch (Exception ex)
            {
                
            }
            return defaultvalue;
        }

        private void SetConnectionString(ConnectTo pConnectDB)
        {
            switch (pConnectDB)
            {
                case ConnectTo.CRM:
                    //get orgunique name and get database name from server
                    _ConnString = GetCRMConnectionString();
                    break;
                case ConnectTo.CustomCRMDB:
                    _ConnString = GetConnectionFromConfig("DevelopmentCustomConnectionString", "ProductionCustomConnectionString");
                    break;
                case ConnectTo.Datawarehouse:
                    _ConnString = GetConnectionFromConfig("DevelopmentDWConnectionString", "ProductionDWConnectionString");
                    break;
                case ConnectTo.GreatPlains:
                    _ConnString = GetConnectionFromConfig("DevelopmentGPConnectionString", "ProductionGPConnectionString");
                    break;
                case ConnectTo.GreatPlainsSW:
                    _ConnString = GetConnectionFromConfig("DevelopmentGPConnectionString", "ProductionGPConnectionString");
                    break;
                case ConnectTo.GreatPlainsBR:
                    _ConnString = GetConnectionFromConfig("DevelopmentGPBRGConnectionString", "ProductionGPBRGConnectionString");
                    break;
                case ConnectTo.CRMAttachmentArchive:
                    _ConnString = GetConnectionFromConfig("DevelopmentCRMAttachmentArchiveConnectionString", "ProductionCRMAttachmentArchiveConnectionString");
                    break;
                case ConnectTo.CreditSuitDB:
                    _ConnString = GetConnectionFromConfig("DevelopmentCreditSuitConnectionString", "ProductionCreditSuitConnectionString");
                    break;
                case ConnectTo.ContechWarehouse:
                    _ConnString = GetConnectionFromConfig("DevelopmentContechWarehouseConnectionString", "ProductionContechWarehouseConnectionString");
                    break;

            }
        }

        public string GetConnectionFromConfig(String pKeyDevelopment, String pKeyProduction)
        {
            try
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsTestMode"].ToString()) == true)
                {
                    if (ConfigurationManager.ConnectionStrings[pKeyDevelopment] != null)
                        return ConfigurationManager.ConnectionStrings[pKeyDevelopment].ToString();
                    else
                        return "";
                }
                else
                {
                    if (ConfigurationManager.ConnectionStrings[pKeyProduction] != null)
                        return ConfigurationManager.ConnectionStrings[pKeyProduction].ToString();
                    else
                        return "";
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                return "";
            }
        }


        private string GetCRMConnectionString()
        {
            String sConn = _ConnString = GetConnectionFromConfig("DevelopmentCRMConnectionString", "ProductionCRMConnectionString");

            //select organization connnection string
            String OrganizationDatabase = "CONTECHConstructionProductsInc_MSCRM";
            OrganizationDatabase = GetCRMOrganizationName(_UniqueOrganizationName);

            sConn = sConn.Replace("MSCRM_CONFIG", OrganizationDatabase);
            //sConn = sConn.Replace("MSCRM_CONFIG", "CCPIQA_MSCRM");

            return sConn;
        }

        public string GetCRMOrganizationName(String pUniqueOrganizationName)
        {
            String sConn = _ConnString = GetConnectionFromConfig("DevelopmentCRMConnectionString", "ProductionCRMConnectionString");

            SqlConnection oConn = new SqlConnection(sConn);
            oConn.Open();

            //select organization connnection string
            String OrganizationDatabase = "CONTECHConstructionProductsInc_MSCRM";

            SqlCommand oCmd = new SqlCommand("SELECT DatabaseName FROM Organization WHERE IsDeleted = 0 AND UniqueName='" + _UniqueOrganizationName + "'", oConn);
            object dbName = oCmd.ExecuteScalar();

            if (dbName != null)
                OrganizationDatabase = dbName.ToString();

            oConn.Close(); //close config connection
            oConn.Dispose();

            return OrganizationDatabase;
        }

        public string GetCRMDatabaseName()
        {
            return ConfigurationManager.AppSettings["CRMDatabaseName"].ToString();
        }

        public string GetCRMOrganizationName()
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsTestMode"].ToString()) == true)
            {
                return ConfigurationManager.AppSettings["DevelopmentCRMOrgName"].ToString();
            }
            else
            {
                return ConfigurationManager.AppSettings["ProductionCRMOrgName"].ToString();
            }
        }
    }
}
