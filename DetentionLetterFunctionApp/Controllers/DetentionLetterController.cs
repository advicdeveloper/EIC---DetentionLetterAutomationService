using System;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DetentionLetterFunctionApp.Controllers
{
    public class DetentionLetterController
    {
        private readonly IDetentionLetterService _detentionLetterService;
        private readonly ILogger<DetentionLetterController> _logger;

        public DetentionLetterController(
            IDetentionLetterService detentionLetterService,
            ILogger<DetentionLetterController> logger)
        {
            _detentionLetterService = detentionLetterService;
            _logger = logger;
        }

        [FunctionName("ProcessDetentionLetters")]
        public async Task Run([TimerTrigger("%TimerSchedule%")] TimerInfo timerInfo)
        {
            _logger.LogInformation($"Detention Letter Timer Function started at: {DateTime.UtcNow}");

            try
            {
                await _detentionLetterService.ProcessPendingDetentionLettersAsync();
                _logger.LogInformation("Detention Letter processing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing detention letters");
                throw;
            }
            finally
            {
                _logger.LogInformation($"Detention Letter Timer Function completed at: {DateTime.UtcNow}");
            }
        }
    }
}
