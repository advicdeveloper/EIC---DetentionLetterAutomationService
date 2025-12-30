using System;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;

namespace DetentionLetterAzureFunction.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateWordReportAsync(Guid salesOrderId, LetterType letterType);
    }
}
