using System.Collections.Generic;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;

namespace DetentionLetterAzureFunction.Services
{
    public interface IEmailService
    {
        Task<bool> SendDetentionLetterEmailAsync(Contact primaryRecipient, List<Contact> ccRecipients, List<byte[]> attachments, string orderNumber);
    }
}
