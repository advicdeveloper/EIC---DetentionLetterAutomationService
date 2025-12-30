# Detention Letter Automation Service - Azure Timer Function

This is an Azure Timer Trigger Function that automates the generation and sending of detention letters for orders. It has been converted from a Windows Service to an Azure Function for better scalability, monitoring, and cloud deployment.

## Overview

The function runs on a configurable schedule (default: every 10 minutes) and performs the following tasks:

1. Retrieves pending detention letters from the database
2. Generates reports for each order
3. Saves reports to the document path
4. Sends emails with attachments to customers
5. Updates order status in the database

## Prerequisites

- .NET 8.0 SDK or later
- Azure Functions Core Tools (for local development)
- Visual Studio 2022 or VS Code with Azure Functions extension
- Access to the CRM database (CustomCRMGP and MSCRM_CONFIG)
- Access to the report generation web service

## Project Structure

```
DetentionLetterAutomationService.AzureFunction/
├── DetentionLetterTimerFunction.cs    # Main timer trigger function
├── DetentionLetterReport.cs            # Report generation and email logic
├── Program.cs                          # Azure Functions host configuration
├── host.json                           # Function app configuration
├── local.settings.json                 # Local development settings (not in source control)
├── LetterDocuments/                    # PDF installation guides
└── README.md                           # This file
```

## Configuration

### Timer Schedule

The timer trigger schedule is configured using NCRONTAB format in the `local.settings.json` file:

```json
"TimerSchedule": "0 */10 * * * *"
```

Common schedule examples:
- Every 10 minutes: `0 */10 * * * *`
- Every 2 minutes: `0 */2 * * * *`
- Daily at 2:00 AM: `0 0 2 * * *`
- Every hour: `0 0 * * * *`

### Application Settings

Configure these settings in `local.settings.json` for local development, or in Azure Portal Application Settings for production:

**Email Settings:**
- `EmailFrom`: Sender email address
- `EmailBcc`: BCC email address
- `EmailHost`: SMTP server hostname
- `EmailHostPort`: SMTP server port
- `EmailSender`: Encrypted email sender username
- `EmailSenderPwd`: Encrypted email sender password

**Integration Settings:**
- `ReportWebURL`: URL for the report generation web service
- `IsTestMode`: Enable/disable test mode

**Connection Strings:**
- `DevelopmentCustomConnectionString`: Development database connection
- `ProductionCustomConnectionString`: Production database connection
- `DevelopmentCRMConnectionString`: Development CRM connection
- `ProductionCRMConnectionString`: Production CRM connection

### local.settings.json Template

Create a `local.settings.json` file with the following structure:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "TimerSchedule": "0 */10 * * * *",
    "EmailFrom": "your-email@domain.com",
    "EmailHost": "your-smtp-server.com",
    "EmailHostPort": "25",
    "ReportWebURL": "https://your-crm-url/ReportExport.aspx"
  },
  "ConnectionStrings": {
    "ProductionCustomConnectionString": "Your connection string here"
  }
}
```

## Building and Running Locally

### Using Azure Functions Core Tools

1. Install dependencies:
   ```bash
   dotnet restore
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the function locally:
   ```bash
   func start
   ```

### Using Visual Studio

1. Open the solution in Visual Studio 2022
2. Set `DetentionLetterAutomationService.AzureFunction` as the startup project
3. Press F5 to run with debugging

## Deploying to Azure

### Prerequisites

- Azure subscription
- Azure Function App created (Linux or Windows)
- Application Insights (optional but recommended)

### Deployment Steps

1. **Using Azure CLI:**
   ```bash
   az login
   az functionapp deployment source config-zip \
     --resource-group <resource-group> \
     --name <function-app-name> \
     --src <zip-file-path>
   ```

2. **Using Visual Studio:**
   - Right-click the project
   - Select "Publish"
   - Follow the wizard to publish to Azure

3. **Using GitHub Actions or Azure DevOps:**
   - Configure CI/CD pipeline
   - Deploy on commit to main branch

### Post-Deployment Configuration

1. Configure Application Settings in Azure Portal
2. Set up connection strings
3. Enable Application Insights for monitoring
4. Configure managed identity if using Azure Key Vault for secrets

## Monitoring and Logging

The function uses `ILogger` for structured logging, which integrates with:

- **Application Insights**: View telemetry, traces, and exceptions
- **Azure Monitor**: Set up alerts and dashboards
- **Log Stream**: Real-time log viewing in Azure Portal

### Key Log Events

- Function execution start/end
- Number of pending detention letters found
- Order processing details
- Email sending status
- Errors and exceptions

## Letter Documents

The following PDF installation guides are included and attached to emails based on report type:

1. NCSPA Installation Manual for CSP, Pipe Arches and Structural Plate
2. CMP Detention Installation Guide
3. DuroMaxx SRPE-Tank Installation Guide
4. DMX Installation Guide
5. DMX Detention Installation Guide

## Error Handling

The function includes comprehensive error handling:

- Individual order processing failures don't stop the entire batch
- Exceptions are logged with full details
- Email validation before sending
- Retry logic can be added using Polly library (future enhancement)

## Performance Considerations

- Timer trigger ensures only one instance runs at a time
- File I/O is optimized for Azure storage
- Connection pooling is configured for database connections
- Attachments are properly disposed after email sending

## Migration from Windows Service

This Azure Function replaces the previous Windows Service with the following improvements:

- **Scalability**: Automatically scales based on load
- **Monitoring**: Built-in Application Insights integration
- **Deployment**: Easy CI/CD and deployment
- **Maintenance**: No need to manage Windows servers
- **Cost**: Pay only for execution time
- **Reliability**: Azure platform handles restarts and failover

## Troubleshooting

### Function not triggering

- Check timer schedule syntax in configuration
- Verify function is not disabled in Azure Portal
- Check Application Insights for any errors

### Email not sending

- Verify SMTP settings and credentials
- Check network connectivity to SMTP server
- Review email validation logic
- Check Application Insights logs

### Database connection issues

- Verify connection strings
- Check firewall rules for Azure Function IP
- Enable Azure SQL managed identity if applicable

## Support

For issues or questions, contact the development team or create an issue in the repository.

## License

Copyright Contech Engineering Solutions
