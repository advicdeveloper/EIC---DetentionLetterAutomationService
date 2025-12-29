using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace DetentionLetterAutomationService.AzureFunctions
{
    /// <summary>
    /// Azure Function alternative for scheduled execution of Detention Letter Plugin
    /// This provides an alternative to Power Automate for organizations that prefer
    /// Azure Functions for scheduled tasks.
    ///
    /// SETUP:
    /// 1. Create new Azure Function App
    /// 2. Add this function to the project
    /// 3. Configure connection string in Application Settings
    /// 4. Deploy to Azure
    /// 5. Monitor via Azure Portal or Application Insights
    ///
    /// NUGET PACKAGES REQUIRED:
    /// - Microsoft.Azure.WebJobs
    /// - Microsoft.PowerPlatform.Dataverse.Client
    /// - Microsoft.Xrm.Sdk
    /// </summary>
    public class DetentionLetterTimerFunction
    {
        /// <summary>
        /// Timer-triggered Azure Function that runs on a schedule
        /// Cron format: {second} {minute} {hour} {day} {month} {day-of-week}
        /// Example below: Runs every 30 minutes
        /// </summary>
        [FunctionName("ProcessDetentionLetters")]
        public async Task Run(
            [TimerTrigger("0 */30 * * * *")] TimerInfo myTimer,
            ILogger log)
        {
            log.LogInformation($"Detention Letter Timer Function executed at: {DateTime.Now}");

            try
            {
                // Get connection string from environment variables
                string connectionString = Environment.GetEnvironmentVariable("DynamicsConnectionString");

                if (string.IsNullOrEmpty(connectionString))
                {
                    log.LogError("DynamicsConnectionString not found in application settings");
                    throw new InvalidOperationException("DynamicsConnectionString not configured");
                }

                // Connect to Dynamics 365
                using (var serviceClient = new ServiceClient(connectionString))
                {
                    if (!serviceClient.IsReady)
                    {
                        log.LogError($"Failed to connect to Dynamics 365: {serviceClient.LastError}");
                        throw new InvalidOperationException($"Connection failed: {serviceClient.LastError}");
                    }

                    log.LogInformation("Successfully connected to Dynamics 365");

                    // Execute the custom action that triggers the plugin
                    await ExecuteDetentionLetterProcessing(serviceClient, log);

                    log.LogInformation("Detention Letter processing completed successfully");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in Detention Letter Timer Function: {ex.Message}");
                log.LogError($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Execute the custom action in Dynamics 365 that triggers the plugin
        /// </summary>
        private async Task ExecuteDetentionLetterProcessing(ServiceClient serviceClient, ILogger log)
        {
            try
            {
                // Option 1: Execute custom action (if you created one)
                var request = new OrganizationRequest("new_ProcessDetentionLetters");
                var response = await Task.Run(() => serviceClient.Execute(request));

                log.LogInformation("Custom action executed successfully");
            }
            catch (Exception ex)
            {
                log.LogError($"Error executing custom action: {ex.Message}");

                // Option 2: Directly instantiate and run the business logic
                // (if custom action approach doesn't work for your scenario)
                log.LogInformation("Attempting direct business logic execution");
                await ExecuteBusinessLogicDirectly(serviceClient, log);
            }
        }

        /// <summary>
        /// Alternative approach: Execute business logic directly without plugin
        /// Use this if you prefer to run the logic from Azure Function instead of D365 plugin
        /// </summary>
        private async Task ExecuteBusinessLogicDirectly(ServiceClient serviceClient, ILogger log)
        {
            try
            {
                log.LogInformation("Executing business logic directly");

                // Import business logic namespaces
                var dLtrrAct = new CONTECH.Service.BusinessLogic.DetentionLetterSummaryAction();
                var lstHistoryIds = dLtrrAct.GetPendingDetentionLetter();

                log.LogInformation($"Found {lstHistoryIds.Count} pending detention letters");

                foreach (var dlOrderSummary in lstHistoryIds)
                {
                    try
                    {
                        log.LogInformation($"Processing order: {dlOrderSummary.OrderNumber}");

                        // Get user details
                        var lstUser = new System.Collections.Generic.List<CONTECH.Service.BusinessEntities.Users>();
                        lstUser.Add(dLtrrAct.GetUserDetail(dlOrderSummary.OrderModifiedBy));

                        string salesorderid = Convert.ToString(dlOrderSummary.OrderId);

                        // Get reports
                        var reportList = new CONTECH.Service.BusinessLogic.GetDetentionLetterReport();
                        var lstReport = reportList.GetReportListForDownload(salesorderid);

                        log.LogInformation($"Order {dlOrderSummary.OrderNumber}: {lstReport.Count} reports to process");

                        if (lstReport.Count > 0)
                        {
                            var dlr = new DetentionLetterReport();

                            // Insert history records
                            foreach (var detentionletter in lstReport)
                            {
                                dlr.InsertOrderHistory(dlOrderSummary, detentionletter.ToString());
                            }

                            // Generate reports
                            dlr.GenerateReport(dlOrderSummary);

                            // Send email if valid
                            if (IsValidEmailAddress(dlOrderSummary.SoldToEmail))
                            {
                                lstUser.Add(dLtrrAct.GetSESell1Detail(dlOrderSummary.OrderNumber));
                                bool isSendEmail = dlr.SendReportEmail(dlOrderSummary, lstUser);

                                if (isSendEmail)
                                {
                                    dlr.UpdateStatus(dlOrderSummary, true, "Successful");
                                    log.LogInformation($"Order {dlOrderSummary.OrderNumber}: Email sent successfully");
                                }
                            }
                            else
                            {
                                dlr.UpdateStatus(dlOrderSummary, true, "SoldTo Email is not exist or Invalid");
                                dlr.SendMissingDetailEmail(dlOrderSummary.OrderModifiedBy, dlOrderSummary.OrderNumber);
                                log.LogWarning($"Order {dlOrderSummary.OrderNumber}: Invalid email address");
                            }
                        }
                        else
                        {
                            var dlr = new DetentionLetterReport();
                            dlr.UpdateStatus(dlOrderSummary, true, "Not Qualified for Letter");
                            log.LogInformation($"Order {dlOrderSummary.OrderNumber}: Not qualified for letter");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Error processing order {dlOrderSummary.OrderNumber}: {ex.Message}");
                        // Continue processing other orders
                    }
                }

                log.LogInformation("Direct business logic execution completed");
            }
            catch (Exception ex)
            {
                log.LogError($"Error in direct business logic execution: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validate email address
        /// </summary>
        private bool IsValidEmailAddress(string address)
        {
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            return address != null && emailAttr.IsValid(address);
        }
    }
}

/*
 * ============================================================================
 * AZURE FUNCTION CONFIGURATION
 * ============================================================================
 *
 * 1. CREATE FUNCTION APP:
 *    - Portal: portal.azure.com
 *    - Create new Function App
 *    - Choose .NET runtime
 *    - Select appropriate hosting plan
 *
 * 2. APPLICATION SETTINGS:
 *    Add the following settings in Configuration:
 *
 *    DynamicsConnectionString:
 *    AuthType=OAuth;
 *    Url=https://yourorg.crm.dynamics.com;
 *    ClientId=<your-app-id>;
 *    ClientSecret=<your-client-secret>;
 *    RedirectUri=http://localhost;
 *
 * 3. TIMER TRIGGER CRON EXPRESSIONS:
 *    - Every 30 minutes: "0 */30 * * * *"
 *    - Every hour:       "0 0 * * * *"
 *    - Every 2 hours:    "0 0 */2 * * *"
 *    - Daily at 2 AM:    "0 0 2 * * *"
 *    - Every weekday:    "0 0 9 * * 1-5"
 *
 * 4. AZURE AD APP REGISTRATION:
 *    - Register app in Azure AD
 *    - Add API permissions for Dynamics 365
 *    - Create client secret
 *    - Grant admin consent
 *    - Create application user in Dynamics 365
 *
 * 5. MONITORING:
 *    - Enable Application Insights
 *    - View logs in Portal or Log Analytics
 *    - Set up alerts for failures
 *
 * 6. DEPLOYMENT:
 *    Options:
 *    a. Visual Studio: Right-click > Publish
 *    b. VS Code: Azure Functions extension
 *    c. Azure CLI: func azure functionapp publish <function-app-name>
 *    d. CI/CD: Azure DevOps or GitHub Actions
 *
 * ============================================================================
 * COMPARISON: Azure Function vs Dynamics 365 Plugin
 * ============================================================================
 *
 * AZURE FUNCTION:
 * + More control over scheduling
 * + Can access external resources easily
 * + Better for long-running processes
 * + Easier debugging and monitoring
 * + Can scale independently
 * - Additional cost (Function App hosting)
 * - Requires Azure AD authentication setup
 * - More moving parts to manage
 *
 * DYNAMICS 365 PLUGIN:
 * + Runs within D365 sandbox (no extra hosting)
 * + Native integration with D365
 * + No separate authentication needed
 * + Better for real-time event processing
 * + No additional Azure resources required
 * - 2-minute execution limit (async)
 * - Limited access to external resources
 * - Harder to debug
 *
 * RECOMMENDATION:
 * - Use D365 Plugin for: Real-time processing, simple workflows
 * - Use Azure Function for: Complex scheduling, long-running tasks, external integrations
 * ============================================================================
 */
