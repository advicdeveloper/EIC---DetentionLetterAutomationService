using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterFunctionApp.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportService> _logger;
        private readonly string _basePath;

        public ReportService(HttpClient httpClient, IConfiguration configuration, ILogger<ReportService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
        }

        public async Task<byte[]> GenerateReportAsync(Guid orderId, string reportName, Guid userId)
        {
            try
            {
                var reportUrl = _configuration["ReportSettings:ReportWebURL"];
                var url = $"{reportUrl}?salesOrderId={orderId}&reportname={reportName}&User={userId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var reportData = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation($"Generated report {reportName} for order {orderId}");

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report {reportName} for order {orderId}");
                return null;
            }
        }

        public async Task<bool> SaveReportToFileAsync(byte[] reportData, string filePath)
        {
            try
            {
                if (reportData == null || reportData.Length == 0)
                {
                    _logger.LogWarning($"No data to save for file {filePath}");
                    return false;
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await File.WriteAllBytesAsync(filePath, reportData);
                _logger.LogInformation($"Saved report to {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving report to {filePath}");
                return false;
            }
        }

        public List<string> GetAdditionalDocumentsForReport(LettersType letterType)
        {
            var documents = new List<string>();

            switch (letterType)
            {
                case LettersType.CMPLargeDiameterLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "NCSPA Installation Manual for CSP, Pipe Arches and Structural Plate.pdf"));
                    break;

                case LettersType.CMPDetentionLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "CMP Detention Installation Guide.pdf"));
                    break;

                case LettersType.DuroMaxxCisternRWHLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "DuroMaxx SRPE-Tank Installation Guide.pdf"));
                    break;

                case LettersType.DuroMaxxContainmentTankNotificationLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "DuroMaxx SRPE-Tank Installation Guide.pdf"));
                    break;

                case LettersType.DuroMaxxLgDiameterLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "DMX Installation Guide.pdf"));
                    break;

                case LettersType.DuroMaxxDetentionLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "DMX Detention Installation Guide.pdf"));
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "CMP Detention Installation Guide.pdf"));
                    break;

                case LettersType.DuroMaxxSewerLetter:
                    documents.Add(Path.Combine(_basePath, "LetterDocuments", "DuroMaxx SRPE-Tank Installation Guide.pdf"));
                    break;
            }

            return documents;
        }
    }
}
