using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;

namespace DetentionLetterFunctionApp.Services
{
    public interface IDynamics365Service
    {
        Task<List<OrderSummary>> GetPendingDetentionLettersAsync();
        Task<User> GetUserByIdAsync(Guid userId);
        Task<User> GetSalesEngineerByOrderNumberAsync(string orderNumber);
        Task<List<LettersType>> GetReportListForOrderAsync(string salesOrderId);
        Task<List<OrderHistory>> GetOrderHistoryAsync(Guid summaryId);
        Task CreateOrderHistoryAsync(OrderHistory orderHistory);
        Task UpdateOrderHistoryAsync(Guid historyId, bool isGenerated, double fileSize, string message);
        Task UpdateOrderSummaryStatusAsync(Guid summaryId, bool isProcessed, string status);
    }
}
