# Detention Letter Automation - Azure Function

This Azure Function automates the processing and delivery of detention letters using Dynamics 365 Online and Azure Functions.

## Architecture

The application follows a clean architecture pattern with separation of concerns:

### Models (`/Models`)
Entity classes representing the business domain:
- `OrderSummary` - Sales order information
- `OrderHistory` - History of detention letter processing
- `User` - User information from Dynamics 365
- `LettersType` - Enumeration of available letter types

### Services (`/Services`)
Business logic and external integrations:
- `IDynamics365Service` / `Dynamics365Service` - Dynamics 365 Online integration via Web API
- `IReportService` / `ReportService` - Report generation and file management
- `IEmailService` / `EmailService` - Email sending functionality
- `IDetentionLetterService` / `DetentionLetterService` - Core business logic orchestration

### Controllers (`/Controllers`)
Entry points for Azure Functions:
- `DetentionLetterController` - Timer-triggered function that processes detention letters

## Process Flow

1. Timer trigger activates the function based on schedule
2. Controller calls the Detention Letter Service
3. Service retrieves pending orders from Dynamics 365
4. For each order:
   - Determines required report types
   - Creates order history records in Dynamics 365
   - Generates PDF reports
   - Saves reports to document path
   - Sends email with attachments
   - Updates order status in Dynamics 365

## Configuration

### Local Development Settings

Configure `local.settings.json` with the following settings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "TimerSchedule": "0 */30 * * * *",
    "Dynamics365:ApiUrl": "https://YOUR_ORG.api.crm.dynamics.com/api/data/v9.2/",
    "Dynamics365:AccessToken": "YOUR_ACCESS_TOKEN",
    "EmailSettings:SmtpServer": "YOUR_SMTP_SERVER",
    "EmailSettings:SmtpPort": "587",
    "EmailSettings:EnableSsl": "true",
    "EmailSettings:FromEmail": "YOUR_FROM_EMAIL",
    "EmailSettings:Username": "YOUR_EMAIL_USERNAME",
    "EmailSettings:Password": "YOUR_EMAIL_PASSWORD",
    "ReportSettings:ReportWebURL": "YOUR_REPORT_SERVER_URL"
  }
}
```

### Azure Application Settings

When deploying to Azure, configure these settings in the Function App:

#### Timer Configuration
- `TimerSchedule` - CRON expression (default: `0 */30 * * * *` for every 30 minutes)

#### Dynamics 365 Settings
- `Dynamics365:ApiUrl` - Dynamics 365 Web API endpoint (e.g., `https://yourorg.api.crm.dynamics.com/api/data/v9.2/`)
- `Dynamics365:AccessToken` - OAuth token for Dynamics 365 authentication

#### Email Settings
- `EmailSettings:SmtpServer` - SMTP server address
- `EmailSettings:SmtpPort` - SMTP port (default: 587)
- `EmailSettings:EnableSsl` - Enable SSL/TLS (true/false)
- `EmailSettings:FromEmail` - Sender email address
- `EmailSettings:Username` - SMTP username
- `EmailSettings:Password` - SMTP password

#### Report Settings
- `ReportSettings:ReportWebURL` - Report server URL for generating detention letter reports

## Timer Schedule

The function uses a CRON expression to define the schedule:
- Default: `0 */30 * * * *` (every 30 minutes)
- Format: `{second} {minute} {hour} {day} {month} {day-of-week}`

Examples:
- Every hour: `0 0 * * * *`
- Every 2 hours: `0 0 */2 * * *`
- Daily at 9 AM: `0 0 9 * * *`

## Letter Types

The system supports the following detention letter types:
- CMP Large Diameter Letter
- CMP Detention Letter
- DuroMaxx Cistern RWH Letter
- DuroMaxx Containment Tank Notification Letter
- DuroMaxx Large Diameter Letter
- DuroMaxx Detention Letter
- DuroMaxx Sewer Letter

Each letter type includes specific installation guide PDFs as attachments.

## Dependencies

- .NET 6.0
- Microsoft.NET.Sdk.Functions
- Microsoft.Azure.Functions.Extensions
- Microsoft.Extensions.Http
- System.Text.Json

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

## Features

1. **Dynamics 365 Integration**: Direct integration with Dynamics 365 Online via Web API (no SQL required)
2. **Timer-Based Processing**: Automatically processes pending detention letters on schedule
3. **Report Generation**: Generates detention letter PDF reports
4. **Email Delivery**: Sends emails with detention letters and installation guides
5. **Status Tracking**: Updates order status in Dynamics 365

## Monitoring

- View logs in Azure Application Insights
- Monitor function executions in Azure Portal
- Check email delivery status in logs

## Security

- Access tokens should be stored in Azure Key Vault
- Use Managed Identity for Azure resource access
- Enable HTTPS only for API calls
- Rotate credentials regularly

## Support

For issues or questions, contact the development team.
