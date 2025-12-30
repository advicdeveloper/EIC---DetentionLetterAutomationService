using System;
using System.Net.Http;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterAzureFunction.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportService> _logger;

        public ReportService(HttpClient httpClient, IConfiguration configuration, ILogger<ReportService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<byte[]> GenerateWordReportAsync(Guid salesOrderId, LetterType letterType)
        {
            try
            {
                var templateUrl = GetTemplateUrl(letterType);
                var url = $"{templateUrl}?salesOrderId={salesOrderId}";

                _logger.LogInformation($"Generating {letterType} report for sales order {salesOrderId}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var reportData = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation($"Successfully generated {letterType} report ({reportData.Length} bytes)");

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating {letterType} report for sales order {salesOrderId}");
                return null;
            }
        }

        private string GetTemplateUrl(LetterType letterType)
        {
            return letterType switch
            {
                LetterType.CMPLetter => _configuration["WordTemplates:CMPLetterUrl"],
                LetterType.DuroMaxxLetter => _configuration["WordTemplates:DuroMaxxLetterUrl"],
                LetterType.UrbanGreenLetter => _configuration["WordTemplates:UrbanGreenLetterUrl"],
                LetterType.LargeDiameterLetter => _configuration["WordTemplates:LargeDiameterLetterUrl"],
                _ => throw new ArgumentException($"Unknown letter type: {letterType}")
            };
        }
    }
}
