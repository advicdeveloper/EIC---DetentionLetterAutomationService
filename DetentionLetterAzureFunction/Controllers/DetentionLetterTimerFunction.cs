using System;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DetentionLetterAzureFunction.Controllers
{
    public class DetentionLetterTimerFunction
    {
        private readonly IDetentionLetterProcessingService _processingService;
        private readonly ILogger<DetentionLetterTimerFunction> _logger;

        public DetentionLetterTimerFunction(
            IDetentionLetterProcessingService processingService,
            ILogger<DetentionLetterTimerFunction> logger)
        {
            _processingService = processingService;
            _logger = logger;
        }

        [FunctionName("ProcessDetentionLetters")]
        public async Task Run([TimerTrigger("%TimerSchedule%")] TimerInfo timerInfo)
        {
            _logger.LogInformation($"Detention Letter Timer Function triggered at: {DateTime.UtcNow}");

            try
            {
                await _processingService.ProcessPendingDetentionLettersAsync();
                _logger.LogInformation("Detention Letter processing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Detention Letter Timer Function");
                throw;
            }
        }
    }
}
