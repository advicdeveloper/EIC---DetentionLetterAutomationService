using System.Collections.Generic;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;

namespace DetentionLetterFunctionApp.Services
{
    public interface IEmailService
    {
        Task<bool> SendDetentionLetterEmailAsync(OrderSummary orderSummary, List<User> ccUsers, List<string> attachmentPaths);
        Task SendMissingEmailNotificationAsync(User user, string orderNumber);
    }
}
