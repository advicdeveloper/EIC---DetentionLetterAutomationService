# Detention Letter Automation - Azure Function

This Azure Function replaces the Windows Service implementation for automated detention letter processing and delivery.

## Overview

This function is triggered on a timer schedule and processes pending detention letters by:
1. Retrieving pending orders from the database
2. Generating detention letter reports
3. Sending emails with appropriate attachments to customers
4. Updating order status in the system

## Project Structure

- `DetentionLetterTimerFunction.cs` - Main timer-triggered function
- `DetentionLetterProcessor.cs` - Core business logic for processing detention letters
- `Logger.cs` - Logging utility using log4net
- `Startup.cs` - Dependency injection configuration

## Configuration

### Local Development

Configure `local.settings.json` with the following settings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "TimerSchedule": "0 */30 * * * *",
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE",
    "EmailSettings:SmtpServer": "YOUR_SMTP_SERVER",
    "EmailSettings:SmtpPort": "587",
    "EmailSettings:EnableSsl": "true",
    "EmailSettings:FromEmail": "YOUR_FROM_EMAIL",
    "EmailSettings:Username": "YOUR_EMAIL_USERNAME",
    "EmailSettings:Password": "YOUR_EMAIL_PASSWORD",
    "CRMReportExecution:Url": "http://psuwconwcrmd01.quikrete.net/ReportServer/ReportExecution2005.asmx"
  }
}
```

### Azure Deployment

Configure the following Application Settings in your Azure Function App:
- `TimerSchedule` - CRON expression for the timer (default: every 30 minutes)
- `ConnectionString` - Database connection string
- `EmailSettings:*` - SMTP configuration for sending emails
- `CRMReportExecution:Url` - CRM report execution URL

## Timer Schedule

The function uses a CRON expression to define the schedule:
- Default: `0 */30 * * * *` (every 30 minutes)
- Format: `{second} {minute} {hour} {day} {month} {day-of-week}`

Examples:
- Every hour: `0 0 * * * *`
- Every 2 hours: `0 0 */2 * * *`
- Daily at 9 AM: `0 0 9 * * *`

## Dependencies

This function references the following existing projects:
- `BusinessEntities` - Data models
- `BusinessLogic` - Business logic layer

## Deployment

### Prerequisites
- .NET 6.0 SDK
- Azure Functions Core Tools (optional, for local testing)
- Azure subscription with Function App created

### Deploy to Azure

1. Build the project:
   ```bash
   dotnet build
   ```

2. Publish to Azure:
   ```bash
   func azure functionapp publish <YourFunctionAppName>
   ```

   Or use Visual Studio's publish functionality.

## Local Testing

1. Configure `local.settings.json` with your settings
2. Run the function locally:
   ```bash
   func start
   ```

   Or press F5 in Visual Studio.

## Logging

- Logs are written to both Azure Application Insights and local log files
- Log files are stored in the `logs/` directory
- Log4net configuration is in `log4net.config`

## Migration from Windows Service

This Azure Function provides the same functionality as the original Windows Service but with the following improvements:
- Cloud-native deployment
- Better scalability
- Integrated monitoring with Application Insights
- Easier configuration management
- No server maintenance required

## Support

For issues or questions, contact the development team.
