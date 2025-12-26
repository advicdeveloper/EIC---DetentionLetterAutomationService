using System.Data;
using System.Data.SqlClient;

namespace CONTECH.Service.DataAccess
{
    public class OrderProductRepositry : Repository
    {
        public DataTable GetOrderProductDetail(string salesorderid)
        {
            DataTable dt = new DataTable();
            try
            {
                OpenConnection(ConnectTo.CustomCRMDB);
                //get produc details
                //string sql = @"SELECT sod.New_ProductSubType as ProductSubType,sod.new_productfamily as ProductFamily,so.OrderNumber as Ordernumber,sod.new_ProductNumber as PartNo,
                //                                            sod.salesorderId as salesOrderId,sod.salesOrderdetailId as salesOrderDetailId,
                //                                            sod.new_pa_gage as Gage,sod.new_pa_grade as Grade,sod.new_pa_corrugation as Corrugation,
                //                                            sod.new_pa_shape as Shape,sod.new_pa_diameter as Diameter,su.FullName as Username,su.InternalEMailAddress as Email
                //                                            FROM salesorderdetail sod WITH(NOLOCK) INNER JOIN
                //                                            salesOrder so WITH(NOLOCK)ON
                //                                            sod.SalesOrderId = so.SalesOrderId INNER JOIN
                //                                            SystemUser su WITH(NOLOCK)ON
                //                                            so.modifiedby = su.SystemUserId
                //                                            where sod.salesorderId ='" + salesorderid + "'";

                string sql = "GetDetentionOrderProducts";

                using (SqlCommand cmd = ConstructCommand(sql))
                {
                    AddInParameter(cmd, "@salesorderid", DbType.Guid, new System.Guid(salesorderid));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }
    }
}
