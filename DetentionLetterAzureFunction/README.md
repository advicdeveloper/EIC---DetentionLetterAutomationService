# Detention Letter Azure Function

Azure Function application for automated detention letter processing based on Dynamics 365 Online sales orders.

## Overview

This application implements the detention letter workflow as specified in the main branch README.md:

1. **Trigger**: Timer-based scheduler retrieves pending detention orders (IsSend = 0)
2. **Validation**: Verifies Business Unit matches required ID
3. **Data Collection**: Retrieves Sales Order, Products, and Contact information
4. **Letter Determination**: Determines applicable detention letters based on Product Family and Part Number
5. **Report Generation**: Generates Word template reports for each letter type
6. **Email Distribution**: Sends reports to Sold-To Contact with Sales Engineers CC'd
7. **Status Update**: Updates IsSend = 1 on successful completion

## Architecture

### Model-Service-Controller Pattern

**Models** (`/Models`):
- `DetentionOrderSummary` - Detention order summary entity from Dataverse
- `SalesOrder` - Sales order information
- `OrderProduct` - Order product details
- `Contact` - Contact/User information
- `LetterType` - Enumeration of letter types

**Services** (`/Services`):
- `IDataverseService` / `DataverseService` - Dataverse/Dynamics 365 Online integration
- `ILetterDeterminationService` / `LetterDeterminationService` - Letter type logic
- `IReportService` / `ReportService` - Word template report generation
- `IEmailService` / `EmailService` - Email sending functionality
- `IDetentionLetterProcessingService` / `DetentionLetterProcessingService` - Main orchestration

**Controllers** (`/Controllers`):
- `DetentionLetterTimerFunction` - Timer-triggered Azure Function

## Business Logic Flow

```
1. Timer Trigger (configurable schedule)
   ↓
2. Retrieve pending detention orders (IsSend = 0)
   ↓
3. For each order:
   ├─ Validate Business Unit = BF3473D3-A652-DE11-B475-001E0B4882E2
   ├─ Retrieve Sales Order details
   ├─ Retrieve Order Products
   ├─ Determine Letter Types (CMP, DuroMaxx, UrbanGreen, LargeDiameter)
   ├─ Generate Word Reports from templates
   ├─ Retrieve Sold-To Contact
   ├─ Retrieve Sales Engineers (based on zip code)
   ├─ Send Email with attachments
   └─ Update IsSend = 1
```

## Configuration

### Dynamics 365 Connection

Configure in `local.settings.json`:

```json
{
  "Dynamics365:ConnectionString": "AuthType=OAuth;Url=https://YOUR_ORG.crm.dynamics.com;ClientId=YOUR_CLIENT_ID;ClientSecret=YOUR_CLIENT_SECRET;",
  "Dynamics365:BusinessUnitId": "BF3473D3-A652-DE11-B475-001E0B4882E2"
}
```

### Email Settings

```json
{
  "EmailSettings:SmtpServer": "smtp.office365.com",
  "EmailSettings:SmtpPort": "587",
  "EmailSettings:EnableSsl": "true",
  "EmailSettings:FromEmail": "noreply@yourcompany.com",
  "EmailSettings:Username": "YOUR_USERNAME",
  "EmailSettings:Password": "YOUR_PASSWORD"
}
```

### Timer Schedule

```json
{
  "TimerSchedule": "0 */30 * * * *"
}
```

CRON Expression Format: `{second} {minute} {hour} {day} {month} {day-of-week}`

Examples:
- Every 30 minutes: `0 */30 * * * *`
- Every hour: `0 0 * * * *`
- Daily at 9 AM: `0 0 9 * * *`

### Word Template URLs

```json
{
  "WordTemplates:CMPLetterUrl": "https://YOUR_TEMPLATE_SERVER/cmp",
  "WordTemplates:DuroMaxxLetterUrl": "https://YOUR_TEMPLATE_SERVER/duromaxx",
  "WordTemplates:UrbanGreenLetterUrl": "https://YOUR_TEMPLATE_SERVER/urbangreen",
  "WordTemplates:LargeDiameterLetterUrl": "https://YOUR_TEMPLATE_SERVER/largediameter"
}
```

## Letter Type Determination Logic

Based on Product Family and Part Number:

| Letter Type | Trigger Conditions |
|-------------|-------------------|
| CMP Letter | Product Family contains "CMP" OR Part Number contains "CMP" |
| DuroMaxx Letter | Product Family contains "DUROMAXX" OR Part Number contains "DMX" |
| Urban Green Letter | Product Family contains "URBANGREEN" OR Part Number contains "UG" |
| Large Diameter Letter | Product Family contains "LARGEDIAMETER" OR Part Number contains "LD" |

## Dataverse Entities

**crmgp_detentionordersummary** (Detention Order Summary):
- `crmgp_detentionordersummaryid` (Primary Key)
- `crmgp_salesorderid` (Lookup to Sales Order)
- `crmgp_soldtocontactid` (Lookup to Contact)
- `crmgp_issend` (0 = Pending, 1 = Sent)
- `crmgp_errormessage` (Error details if failed)
- `crmgp_processedon` (Timestamp when processed)

## Error Handling

- Failed records remain with `IsSend = 0`
- Error message stored in `crmgp_errormessage` field
- Automatic retry on next scheduler run
- Comprehensive logging to Application Insights

## Deployment

### Prerequisites

- .NET 6.0 SDK
- Azure subscription
- Azure Function App (Consumption or Premium plan)
- Dynamics 365 Online instance
- SMTP server for email

### Steps

1. **Build the project**:
   ```bash
   dotnet build DetentionLetterAzureFunction.sln --configuration Release
   ```

2. **Publish to Azure**:
   ```bash
   cd DetentionLetterAzureFunction
   func azure functionapp publish <YOUR_FUNCTION_APP_NAME>
   ```

3. **Configure Application Settings** in Azure Portal:
   - Dynamics365:ConnectionString
   - Dynamics365:BusinessUnitId
   - EmailSettings:*
   - TimerSchedule
   - WordTemplates:*

4. **Enable Application Insights** for monitoring and logging

## Local Development

1. Install Azure Functions Core Tools
2. Configure `local.settings.json`
3. Run locally:
   ```bash
   cd DetentionLetterAzureFunction
   func start
   ```

## Monitoring

- View execution logs in Azure Application Insights
- Monitor function invocations in Azure Portal
- Track email delivery success/failure rates
- Review error messages in Dataverse records

## Security Best Practices

- Store connection strings in Azure Key Vault
- Use Managed Identity for Azure resource access
- Enable HTTPS only
- Rotate credentials regularly
- Implement IP restrictions if needed

## Dependencies

- Microsoft.NET.Sdk.Functions 4.2.0
- Microsoft.Azure.Functions.Extensions 1.1.0
- Microsoft.PowerPlatform.Dataverse.Client 1.0.30
- Microsoft.Extensions.Http 6.0.0
- System.Text.Json 6.0.0

## Support

For questions or issues, contact the development team.
