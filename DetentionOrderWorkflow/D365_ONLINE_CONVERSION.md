# Dynamics 365 Online Conversion Guide

## Overview

This custom action has been designed to work with **Dynamics 365 Online** using the **CRM SDK** and **FetchXML queries**, replacing the SQL Server stored procedures used in on-premise deployments.

## Stored Procedure Conversions

### 1. GetDetentionOrderProducts → FetchXML Query

**Original SQL Stored Procedure:**
```sql
SELECT sod.New_ProductSubType, sod.new_productfamily, so.OrderNumber,
       sod.new_ProductNumber, sod.new_pa_gage, sod.new_pa_grade,
       sod.new_pa_corrugation, sod.new_pa_shape, sod.new_pa_diameter,
       su.FullName, su.InternalEMailAddress
FROM SalesOrderDetailBase sod
INNER JOIN SalesOrderBase so ON sod.SalesOrderId = so.SalesOrderId
INNER JOIN SystemUserBase su ON so.modifiedby = su.SystemUserId
WHERE sod.salesorderId = @salesorderId
```

**Converted to FetchXML:**
```xml
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='salesorderdetail'>
    <attribute name='salesorderdetailid' />
    <attribute name='new_productsubtype' />
    <attribute name='new_productfamily' />
    <attribute name='new_productnumber' />
    <attribute name='new_pa_gage' />
    <attribute name='new_pa_grade' />
    <attribute name='new_pa_corrugation' />
    <attribute name='new_pa_shape' />
    <attribute name='new_pa_diameter' />
    <link-entity name='salesorder' from='salesorderid' to='salesorderid' alias='so'>
      <attribute name='ordernumber' />
      <attribute name='modifiedby' />
      <link-entity name='systemuser' from='systemuserid' to='modifiedby' alias='su'>
        <attribute name='fullname' />
        <attribute name='internalemailaddress' />
      </link-entity>
    </link-entity>
    <filter type='or'>
      <condition attribute='salesorderid' operator='eq' value='{GUID}' />
    </filter>
  </entity>
</fetch>
```

**Implementation Location:** `GetWordTemplateList` method, line 149

---

### 2. SnowflakeOrderSplit Query → FetchXML Query

**Original SQL Logic:**
```sql
Where sod.salesorderId in(
  Select OrderId from SnowflakeOrderSplit
  where OrderId = @salesorderId or SnowflakeOrderId = @salesorderId
) or so.SalesOrderId = @salesorderId
```

**Converted to FetchXML:**
```xml
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
  <entity name='new_snowflakeordersplit'>
    <attribute name='new_orderid' />
    <attribute name='new_snowflakeorderid' />
    <filter type='or'>
      <condition attribute='new_orderid' operator='eq' value='{GUID}' />
      <condition attribute='new_snowflakeorderid' operator='eq' value='{GUID}' />
    </filter>
  </entity>
</fetch>
```

**Implementation Location:** `GetWordTemplateList` method, line 182

---

### 3. GetUserDetailForDetentionLetter → FetchXML Query

**Original SQL Stored Procedure:**
```sql
Select U.* from SystemUserBase U
INNER JOIN New_zipcode Z on Z.new_senameid = U.SystemUserId
INNER JOIN SalesCommission vs on Z.New_zipcode = vs.SESell1Zipcode
WHERE vs.MerlinOrderNo = @OrderNumber
```

**Converted to FetchXML:**
```xml
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' top='1'>
  <entity name='systemuser'>
    <attribute name='systemuserid' />
    <attribute name='fullname' />
    <attribute name='domainname' />
    <attribute name='internalemailaddress' />
    <attribute name='title' />
    <link-entity name='new_zipcode' from='new_senameid' to='systemuserid' alias='zipcode'>
      <attribute name='new_zipcode' />
      <link-entity name='new_salescommission' from='new_sesell1zipcode' to='new_zipcode' alias='commission'>
        <filter>
          <condition attribute='new_merlinorderno' operator='eq' value='ORDER_NUMBER' />
        </filter>
      </link-entity>
    </link-entity>
  </entity>
</fetch>
```

**Implementation Location:** `GetSESell1UserDetails` method, line 484

---

## Entity Schema Name Mapping

### Standard Entities

| SQL Table Name | D365 Entity Name | Description |
|----------------|------------------|-------------|
| SalesOrderBase | salesorder | Sales Order header |
| SalesOrderDetailBase | salesorderdetail | Sales Order line items |
| SystemUserBase | systemuser | CRM Users |

### Custom Entities (May need adjustment based on your D365 configuration)

| SQL Table Name | D365 Entity Name | Description |
|----------------|------------------|-------------|
| SnowflakeOrderSplit | new_snowflakeordersplit | Split order tracking |
| New_zipcode | new_zipcode | Custom zipcode entity |
| SalesCommission | new_salescommission | Sales commission data |

**⚠️ Important:** Verify custom entity schema names in your D365 environment. They may have different prefixes.

---

## Field Name Mapping

### Sales Order Detail Fields

| SQL Column Name | D365 Attribute Name | Type |
|-----------------|---------------------|------|
| New_ProductSubType | new_productsubtype | String |
| new_productfamily | new_productfamily | String |
| new_ProductNumber | new_productnumber | String |
| new_pa_gage | new_pa_gage | String |
| new_pa_grade | new_pa_grade | String |
| new_pa_corrugation | new_pa_corrugation | String |
| new_pa_shape | new_pa_shape | String |
| new_pa_diameter | new_pa_diameter | String |

### System User Fields

| SQL Column Name | D365 Attribute Name | Type |
|-----------------|---------------------|------|
| FullName | fullname | String |
| DomainName | domainname | String |
| InternalEMailAddress | internalemailaddress | String |
| Title | title | String |
| SystemUserId | systemuserid | Guid |

---

## Key Implementation Differences

### 1. No Direct Database Access

**On-Premise (SQL):**
```csharp
SqlCommand cmd = new SqlCommand("GetDetentionOrderProducts");
SqlDataAdapter da = new SqlDataAdapter(cmd);
da.Fill(dt);
```

**D365 Online (CRM SDK):**
```csharp
EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));
foreach (Entity product in results.Entities)
{
    string productFamily = product.GetAttributeValue<string>("new_productfamily");
}
```

### 2. Entity Access Pattern

**Accessing Entity Attributes:**
```csharp
// Safe attribute access with null handling
string value = entity.Contains("attributename") ?
    entity.GetAttributeValue<string>("attributename") : string.Empty;

// For GUIDs
Guid guidValue = entity.GetAttributeValue<Guid>("attributename");

// For integers
int intValue = entity.GetAttributeValue<int>("attributename");
```

### 3. FetchXML Best Practices

**Limit Results:**
```xml
<fetch top='1'>  <!-- Limits to 1 record -->
```

**Distinct Results:**
```xml
<fetch distinct='true'>  <!-- Removes duplicates -->
```

**Aliasing Linked Entities:**
```xml
<link-entity name='systemuser' alias='su'>
  <attribute name='fullname' />
</link-entity>
```

Access aliased attributes: `entity.GetAttributeValue<AliasedValue>("su.fullname").Value`

---

## Testing in D365 Online

### 1. Verify Custom Entity Names

Before deploying, verify your custom entity schema names:

1. Go to **Settings** → **Customizations** → **Customize the System**
2. Expand **Entities**
3. Find your custom entities:
   - Snowflake Order Split
   - Zipcode
   - Sales Commission
4. Note the **Name** field (e.g., `new_snowflakeordersplit`)

### 2. Test FetchXML Queries

Use **FetchXML Builder** (XrmToolBox) to test queries:

1. Install XrmToolBox
2. Add FetchXML Builder tool
3. Paste the FetchXML from the code
4. Click "Execute"
5. Verify results

### 3. Test the Custom Action

1. Deploy the assembly to D365 Online
2. Create a test sales order with products
3. Create a detention order summary record
4. Trigger the Power Automate flow
5. Check Plugin Trace Logs for execution details

---

## Error Handling

### Split Order Table Not Found

The code gracefully handles missing split order table:

```csharp
try
{
    // Query split orders
}
catch (Exception exSplit)
{
    tracingService.Trace($"Note: Split order table not found: {exSplit.Message}");
    // Continue without split order data
}
```

### User Details Not Found

If SESell1 user is not found, the workflow continues with empty user fields:

```csharp
catch (Exception ex)
{
    tracingService.Trace($"Error getting user details: {ex.Message}");
    userDetails = null;  // Continue with null
}
```

---

## Performance Considerations

### 1. FetchXML Query Limits

- Default page size: 5000 records
- Maximum: 50,000 records per query
- For large datasets, use paging

### 2. Link-Entity Performance

- Each link-entity adds a JOIN operation
- Minimize link-entities when possible
- Use top='N' to limit results

### 3. Attribute Selection

Only select attributes you need:

```xml
<!-- Good - specific attributes -->
<attribute name='fullname' />
<attribute name='internalemailaddress' />

<!-- Avoid - all attributes -->
<all-attributes />
```

---

## Deployment Checklist

- [ ] Verify custom entity schema names in your D365 environment
- [ ] Verify custom field schema names
- [ ] Test FetchXML queries in FetchXML Builder
- [ ] Build and sign the assembly with strong name key
- [ ] Register assembly using Plugin Registration Tool
- [ ] Create custom action process in D365
- [ ] Test with sample data
- [ ] Review Plugin Trace Logs for errors
- [ ] Configure Power Automate flow
- [ ] End-to-end testing

---

## Troubleshooting

### Error: "Entity 'new_snowflakeordersplit' not found"

**Solution:** Update the entity name in the FetchXML to match your environment, or remove split order logic if not used.

### Error: "Attribute 'new_productnumber' not found"

**Solution:** Verify the field schema name in your D365 environment and update the FetchXML accordingly.

### No results returned

**Solution:**
1. Check Plugin Trace Log for FetchXML query
2. Test the FetchXML in FetchXML Builder
3. Verify data exists in the entities
4. Check filter conditions

### Performance issues

**Solution:**
1. Add `top='N'` to limit results
2. Remove unnecessary link-entities
3. Only select required attributes
4. Add indexes to frequently queried fields (via D365 admin)

---

## Migration Path from On-Premise to Online

If migrating from on-premise to online:

1. **Phase 1:** Deploy this custom action alongside existing stored procedure approach
2. **Phase 2:** Test thoroughly in a sandbox environment
3. **Phase 3:** Update Power Automate flows to use the new custom action
4. **Phase 4:** Monitor Plugin Trace Logs for any issues
5. **Phase 5:** Decommission old stored procedure-based approach

---

## Support

For issues specific to D365 Online deployment:
- Check Plugin Trace Logs: **Settings** → **Plug-in Trace Log**
- Use FetchXML Builder to debug queries
- Verify entity and field schema names in your environment
- Test with small datasets first
