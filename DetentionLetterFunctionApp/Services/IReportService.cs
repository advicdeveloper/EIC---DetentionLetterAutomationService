using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;

namespace DetentionLetterFunctionApp.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateReportAsync(Guid orderId, string reportName, Guid userId);
        Task<bool> SaveReportToFileAsync(byte[] reportData, string filePath);
        List<string> GetAdditionalDocumentsForReport(LettersType letterType);
    }
}
