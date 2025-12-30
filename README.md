# Detention Order Summary – Detailed Functional Documentation

## 1\. Trigger Condition

When a Sales Order status is set to Fulfilled and Status Reason is Completed, the process is initiated.

## 2\. Business Unit Validation

The system checks if the Owning Business Unit equals:

BF3473D3-A652-DE11-B475-001E0B4882E2

Only if this condition is met, processing continues.

## 3\. Data Collection

The following related records are retrieved:

\- Sales Order

\- Opportunity

\- Quote

\- Sold-To Contact

\- Snowflake Order Split

## 4\. Detention Order Summary Creation

A new record is created in the Dataverse entity:

crmgp\_detentionordersummary

Initially, IsSend is set to 0 (Pending).

## 5\. Scheduler Execution

A scheduled job retrieves records from crmgp\_detentionordersummary where IsSend = 0.

## 6\. Order Processing Loop

Each pending detention order summary record is processed individually.

## 7\. Product Retrieval

Order product details are retrieved for the Sales Order.

## 8\. Letter Type Determination

Based on Product Family and Part Number logic, the system determines applicable detention letters including CMP, DuroMaxx, Urban Green, and Large Diameter letters.

## 9\. Report Generation

Word templates are executed using Sales Order ID.

## 10\. Email Recipient Resolution

Primary Recipient:

Sold-To Contact Email

CC Recipients determined using Sales Order Zip Code (SE).

Duplicate email addresses are removed.

Attached the word templates

## 11\. Email Sending

Generated reports are attached and sent via email.

## 12\. Status Update

After successful email sending, IsSend is updated to 1.

## 13\. Error Handling

Errors are logged and failed records are retried in the next scheduler run.
