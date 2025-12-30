using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;

namespace DetentionLetterAzureFunction.Services
{
    public interface IDataverseService
    {
        Task<List<DetentionOrderSummary>> GetPendingDetentionOrdersAsync();
        Task<SalesOrder> GetSalesOrderAsync(Guid salesOrderId);
        Task<List<OrderProduct>> GetOrderProductsAsync(Guid salesOrderId);
        Task<Contact> GetContactAsync(Guid contactId);
        Task<List<Contact>> GetSalesEngineersAsync(string zipCode);
        Task UpdateDetentionOrderStatusAsync(Guid detentionOrderId, int isSend, string errorMessage = null);
    }
}
