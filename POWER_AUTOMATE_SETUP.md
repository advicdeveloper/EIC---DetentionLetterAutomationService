# Power Automate Flow Setup Guide

## Overview

This guide provides step-by-step instructions for setting up the Power Automate flow that integrates with the Detention Order custom workflow action.

## Prerequisites

1. Custom workflow action `GetDetentionOrderDataAction` must be deployed to Dynamics 365
2. Custom action must be registered and activated in Dynamics 365
3. User must have permissions to create Power Automate flows

## Flow Configuration

### Step 1: Create a New Flow

1. Go to Power Automate (https://make.powerautomate.com)
2. Click "Create" → "Automated cloud flow"
3. Name: "Detention Order - Process Summary"
4. Trigger: "When a record is created, updated or deleted"
5. Click "Create"

### Step 2: Configure the Trigger

Configure the trigger with the following settings:

- **Change type**: Added
- **Table name**: Detention Order Summaries (crmgp_detentionordersummary)
- **Scope**: Organization
- **Run as**: User who created the flow

**Select columns** (optional, for performance):
- crmgp_detentionordersummaryid
- crmgp_orderid
- crmgp_ordernumber
- crmgp_name

### Step 3: Add Custom Action

1. Click "New step"
2. Search for "Perform an unbound action"
3. Select "Perform an unbound action"
4. Configure the action:

**Action Name**: `new_GetDetentionOrderData`

**Input Parameters**:
```
SalesOrderId: @{triggerOutputs()?['body/crmgp_orderid']}
OrderNumber: @{triggerOutputs()?['body/crmgp_ordernumber']}
```

> **Note**: Replace the field names above with the actual schema names from your Dynamics 365 environment.

### Step 4: Add Condition to Check Success

1. Click "New step"
2. Search for "Condition"
3. Configure:

**Left side**: `@{outputs('Perform_an_unbound_action')?['body/Success']}`
**Operator**: is equal to
**Right side**: `true`

### Step 5: Configure Success Branch

In the "If yes" branch, add the following actions:

#### A. Initialize Word Templates Variable

1. Add action: "Initialize variable"
2. Configure:
   - **Name**: `WordTemplates`
   - **Type**: String
   - **Value**: `@{outputs('Perform_an_unbound_action')?['body/WordTemplates']}`

#### B. Initialize User Email Variable

1. Add action: "Initialize variable"
2. Configure:
   - **Name**: `UserEmail`
   - **Type**: String
   - **Value**: `@{outputs('Perform_an_unbound_action')?['body/UserEmail']}`

#### C. Compose Word Template List (for logging/debugging)

1. Add action: "Compose"
2. Configure:
   - **Inputs**:
   ```json
   {
     "WordTemplates": "@{variables('WordTemplates')}",
     "WordTemplatesCount": "@{outputs('Perform_an_unbound_action')?['body/WordTemplatesCount']}",
     "UserEmail": "@{variables('UserEmail')}",
     "UserName": "@{outputs('Perform_an_unbound_action')?['body/UserName']}",
     "UserFullName": "@{outputs('Perform_an_unbound_action')?['body/UserFullName']}",
     "UserTitle": "@{outputs('Perform_an_unbound_action')?['body/UserTitle']}"
   }
   ```

#### D. Add Your Business Logic Here

This is where you would add additional steps such as:
- Using the Word Templates list to generate appropriate documents
- Sending emails to the user
- Updating records
- etc.

**Example: Generate Documents Based on Word Templates**

The custom action returns specific template names that you can use directly:

```
Templates could include:
- CMPDetentionLetter
- CMPLargeDiameterLetter
- DuroMaxxDetentionLetter
- DuroMaxxLgDiameterLetter
- DuroMaxxCisternRWHLetter
- DuroMaxxContainmentTankNotificationLetter
- DuroMaxxSewerLetter
- ArmortecSubmittal

You can use Apply to Each to iterate through the templates:
For Each template in WordTemplates.Split(',')
  Generate document using template
  Attach to email or save to SharePoint
```

### Step 6: Configure Failure Branch

In the "If no" branch, handle errors:

#### A. Send Email Notification

1. Add action: "Send an email (V2)"
2. Configure:
   - **To**: admin@yourcompany.com
   - **Subject**: `Error in Detention Order Processing`
   - **Body**:
   ```
   An error occurred while processing the detention order.

   Order Number: @{triggerOutputs()?['body/crmgp_ordernumber']}
   Sales Order Id: @{triggerOutputs()?['body/crmgp_orderid']}

   Error Message:
   @{outputs('Perform_an_unbound_action')?['body/ErrorMessage']}
   ```

#### B. Update Record Status (Optional)

1. Add action: "Update a record"
2. Configure to update the detention order summary with error status

### Step 7: Save and Test

1. Click "Save" to save the flow
2. Turn on the flow
3. Test by creating a new record in `crmgp_detentionordersummary`

## Complete Flow JSON (Reference)

Here's a sample flow configuration in JSON format:

```json
{
  "definition": {
    "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
    "actions": {
      "Get_Detention_Order_Data": {
        "type": "ApiConnection",
        "inputs": {
          "host": {
            "connectionName": "shared_commondataserviceforapps",
            "operationId": "PerformUnboundAction"
          },
          "parameters": {
            "actionName": "new_GetDetentionOrderData",
            "item/SalesOrderId": "@triggerOutputs()?['body/crmgp_orderid']",
            "item/OrderNumber": "@triggerOutputs()?['body/crmgp_ordernumber']"
          }
        }
      },
      "Condition_Check_Success": {
        "type": "If",
        "expression": {
          "and": [
            {
              "equals": [
                "@outputs('Get_Detention_Order_Data')?['body/Success']",
                true
              ]
            }
          ]
        },
        "actions": {
          "Initialize_ProductFamilies": {
            "type": "InitializeVariable",
            "inputs": {
              "variables": [
                {
                  "name": "ProductFamilies",
                  "type": "string",
                  "value": "@{outputs('Get_Detention_Order_Data')?['body/ProductFamilies']}"
                }
              ]
            }
          }
        },
        "else": {
          "actions": {
            "Send_Error_Email": {
              "type": "ApiConnection",
              "inputs": {
                "host": {
                  "connectionName": "shared_office365",
                  "operationId": "SendEmailV2"
                },
                "parameters": {
                  "emailMessage/To": "admin@yourcompany.com",
                  "emailMessage/Subject": "Error in Detention Order Processing",
                  "emailMessage/Body": "Error: @{outputs('Get_Detention_Order_Data')?['body/ErrorMessage']}"
                }
              }
            }
          }
        }
      }
    },
    "triggers": {
      "When_record_created": {
        "type": "ApiConnectionWebhook",
        "inputs": {
          "host": {
            "connectionName": "shared_commondataserviceforapps",
            "operationId": "SubscribeWebhookTrigger"
          },
          "parameters": {
            "subscriptionRequest/message": 1,
            "subscriptionRequest/entityname": "crmgp_detentionordersummary",
            "subscriptionRequest/scope": 4
          }
        }
      }
    }
  }
}
```

## Output Parameter Reference

After the custom action runs successfully, you can access these outputs:

| Output Parameter | Expression | Example Value |
|-----------------|------------|---------------|
| Word Templates | `@{outputs('Get_Detention_Order_Data')?['body/WordTemplates']}` | "CMPDetentionLetter, DuroMaxxDetentionLetter" |
| Word Templates Count | `@{outputs('Get_Detention_Order_Data')?['body/WordTemplatesCount']}` | 2 |
| User Email | `@{outputs('Get_Detention_Order_Data')?['body/UserEmail']}` | "john.doe@company.com" |
| User Name | `@{outputs('Get_Detention_Order_Data')?['body/UserName']}` | "jdoe" |
| User Full Name | `@{outputs('Get_Detention_Order_Data')?['body/UserFullName']}` | "John Doe" |
| User Title | `@{outputs('Get_Detention_Order_Data')?['body/UserTitle']}` | "Sales Engineer" |
| Success | `@{outputs('Get_Detention_Order_Data')?['body/Success']}` | true |
| Error Message | `@{outputs('Get_Detention_Order_Data')?['body/ErrorMessage']}` | "" |

## Common Use Cases

### Use Case 1: Generate Documents for Each Template

```
Apply to each: @{split(variables('WordTemplates'), ', ')}
  Current item = Template Name (e.g., "CMPDetentionLetter")

  Action: Generate document from template
  Template Name: @{items('Apply_to_each')}

  Action: Save document or attach to email
```

### Use Case 2: Send Notification to SESell1 User

```
Send Email (V2)
To: @{outputs('Get_Detention_Order_Data')?['body/UserEmail']}
Subject: Detention Order Notification - Documents Ready
Body: Dear @{outputs('Get_Detention_Order_Data')?['body/UserFullName']},

      Your detention order has been processed.
      The following documents have been generated:
      @{outputs('Get_Detention_Order_Data')?['body/WordTemplates']}

      Total documents: @{outputs('Get_Detention_Order_Data')?['body/WordTemplatesCount']}
```

### Use Case 3: Update CRM Record with Template Info

```
Update a record (Dynamics 365)
Table: Detention Order Summaries
Record ID: @{triggerOutputs()?['body/crmgp_detentionordersummaryid']}
Fields:
  - Templates Generated: @{outputs('Get_Detention_Order_Data')?['body/WordTemplates']}
  - Template Count: @{outputs('Get_Detention_Order_Data')?['body/WordTemplatesCount']}
  - Assigned User: @{outputs('Get_Detention_Order_Data')?['body/UserEmail']}
  - Processing Status: "Completed"
```

## Debugging Tips

1. **Enable Flow Run History**: Always keep flow run history enabled for troubleshooting
2. **Use Compose Actions**: Add Compose actions to see intermediate values
3. **Check Trace Logs**: View Dynamics 365 Plugin Trace Logs for custom action execution details
4. **Test with Known Data**: Use a test sales order with predictable product families

## Performance Considerations

- The custom action executes database queries, so response time depends on:
  - Number of products in the sales order
  - Database performance
  - Network latency

- For large orders (100+ products), consider:
  - Adding timeout handling in your flow
  - Implementing pagination if needed in the future

## Security

- The custom action runs in the context of the Dynamics 365 system user
- Ensure the service account has appropriate database permissions
- Output parameters may contain sensitive user information - handle accordingly

## Support

For issues with:
- **Custom Action**: Check Plugin Trace Logs in Dynamics 365
- **Power Automate Flow**: Check Flow Run History
- **Database Queries**: Contact database administrator

## Next Steps

After setting up the basic flow:
1. Add document generation logic
2. Configure email templates
3. Set up error notification routing
4. Add audit logging if required
5. Test with production-like data
