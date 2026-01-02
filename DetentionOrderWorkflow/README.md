# Detention Order Workflow - Custom Action for Dynamics 365

## Overview

This project contains a custom Dynamics 365 workflow action that can be called from Power Automate to retrieve detention order data. The action processes sales order information and returns product families and user details.

## Purpose

When a record is created in `crmgp_detentionordersummary`, Power Automate can call this custom action to:
1. Get all sales order products for the specified order
2. Extract unique product families (for Word template selection)
3. Retrieve SESell1 user details
4. Return structured data for further processing

## Input Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| Sales Order Id | String (GUID) | Yes | The unique identifier of the sales order |
| Order Number | String | No | The order number for retrieving user details |

## Output Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Product Families | String | Comma-separated list of unique product families |
| Product Families Count | Integer | Number of unique product families found |
| User Email | String | Email address of the SESell1 user |
| User Name | String | Username of the SESell1 user |
| User Full Name | String | Full name of the SESell1 user |
| User Title | String | Job title of the SESell1 user |
| Success | Boolean | Indicates if the action executed successfully |
| Error Message | String | Error details if the action failed |

## Deployment Instructions

### 1. Build the Assembly

Before deploying, you need to build the assembly in Visual Studio:

1. Open `DetentionLetterAutomationService.sln` in Visual Studio
2. Right-click on the `DetentionOrderWorkflow` project
3. Select "Build" from the context menu
4. The compiled DLL will be in `DetentionOrderWorkflow\bin\Release\`

### 2. Generate Strong Name Key (First Time Only)

If the `key.snk` file doesn't exist, generate it:

```bash
cd DetentionOrderWorkflow
sn -k key.snk
```

Note: This requires Visual Studio Developer Command Prompt.

### 3. Register the Assembly in Dynamics 365

Use the Plugin Registration Tool to register the assembly:

1. Open the Plugin Registration Tool
2. Click "Register" → "Register New Assembly"
3. Browse to `CONTECH.Service.DetentionOrderWorkflow.dll`
4. Select the assembly and click "Register Selected Plugins"
5. The `GetDetentionOrderDataAction` workflow will be registered

### 4. Create Custom Action in Dynamics 365

1. Go to Settings → Processes in Dynamics 365
2. Click "New" to create a new process
3. Fill in the details:
   - **Process Name**: Get Detention Order Data
   - **Category**: Action
   - **Entity**: None (Global)
4. Add the workflow step to call `GetDetentionOrderDataAction`
5. Map the input and output parameters
6. Activate the action

## Power Automate Integration

### Flow Structure

```
Trigger: When a record is created in crmgp_detentionordersummary
  ↓
Action: Get Detention Order Data (Custom Action)
  - Input: Sales Order Id from trigger
  - Input: Order Number from trigger
  ↓
Condition: Check if Success = true
  ↓
Yes Branch:
  - Use Product Families for template selection
  - Use User Email, User Name, etc. for notifications
  - Continue with document generation
  ↓
No Branch:
  - Log Error Message
  - Send notification to admin
```

### Example Power Automate Configuration

#### Step 1: Trigger
- **Trigger**: When a record is created
- **Entity**: crmgp_detentionordersummary

#### Step 2: Call Custom Action
- **Action**: Perform an unbound action
- **Action Name**: new_GetDetentionOrderData
- **Input Parameters**:
  - SalesOrderId: `{triggerOutputs()?['body/crmgp_orderid']}`
  - OrderNumber: `{triggerOutputs()?['body/crmgp_ordernumber']}`

#### Step 3: Use Output Parameters
The custom action returns the following that you can use in subsequent steps:

```javascript
// Product information
@{outputs('Get_Detention_Order_Data')?['body/ProductFamilies']}
@{outputs('Get_Detention_Order_Data')?['body/ProductFamiliesCount']}

// User information
@{outputs('Get_Detention_Order_Data')?['body/UserEmail']}
@{outputs('Get_Detention_Order_Data')?['body/UserName']}
@{outputs('Get_Detention_Order_Data')?['body/UserFullName']}
@{outputs('Get_Detention_Order_Data')?['body/UserTitle']}

// Status
@{outputs('Get_Detention_Order_Data')?['body/Success']}
@{outputs('Get_Detention_Order_Data')?['body/ErrorMessage']}
```

## Logic Implementation

### Unique Product Families

The action retrieves all products for a sales order and extracts unique product families:

1. Queries `salesorderdetail` table via stored procedure `GetDetentionOrderProducts`
2. Extracts `ProductFamily` column from all rows
3. Removes duplicates using HashSet
4. Sorts alphabetically
5. Returns as comma-separated string

This unique list can be used to determine which Word templates to use for document generation.

### SESell1 User Details

The action retrieves user details based on the order number:

1. Calls stored procedure `GetUserDetailForDetentionLetter`
2. Returns a single user record with:
   - Email address
   - Username
   - Full name
   - Job title

The stored procedure ensures that the modified by user is unique and returns the appropriate user for the order.

## Error Handling

The custom action includes comprehensive error handling:

- All exceptions are caught and logged to the Dynamics 365 trace log
- The `Success` output parameter is set to `false` on error
- The `ErrorMessage` output parameter contains the error details
- The action throws `InvalidPluginExecutionException` for Dynamics 365 to handle

In Power Automate, you should:
1. Check the `Success` parameter after calling the action
2. Branch your flow based on success/failure
3. Log or handle errors appropriately

## Database Dependencies

This custom action depends on the following database objects:

### Stored Procedures
- `GetDetentionOrderProducts` - Retrieves sales order products with product family information
- `GetUserDetailForDetentionLetter` - Retrieves SESell1 user details for an order

### Tables
- `salesorderdetail` - Contains product information
- `salesorder` - Contains order header information
- `SystemUserBase` - Contains user information

## Testing

To test the custom action:

1. Create a test sales order with multiple products from different product families
2. Create a record in `crmgp_detentionordersummary` for that order
3. The Power Automate flow should trigger automatically
4. Verify the custom action returns:
   - Unique product families
   - Correct user details
   - Success = true

## Troubleshooting

### Common Issues

1. **Assembly not loading**
   - Ensure the assembly is signed with a strong name key
   - Check that all dependent assemblies are available

2. **Database connection errors**
   - Verify connection strings in the DataAccess project
   - Ensure the service account has appropriate database permissions

3. **User details not found**
   - Verify the order number is correct
   - Check that the stored procedure `GetUserDetailForDetentionLetter` returns data

4. **Product families empty**
   - Verify the sales order has products
   - Check that products have the `ProductFamily` field populated

### Viewing Trace Logs

To view detailed execution logs:
1. Go to Settings → Plug-in Trace Log in Dynamics 365
2. Filter by `CONTECH.Service.DetentionOrderWorkflow`
3. Review the trace messages for execution details

## Version History

- **v1.0.0** - Initial release
  - Get unique product families from sales order
  - Retrieve SESell1 user details
  - Return structured data for Power Automate

## Support

For issues or questions, contact the development team or create an issue in the Azure DevOps project.
