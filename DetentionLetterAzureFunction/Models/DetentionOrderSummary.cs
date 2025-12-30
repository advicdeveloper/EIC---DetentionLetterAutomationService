using System;

namespace DetentionLetterAzureFunction.Models
{
    public class DetentionOrderSummary
    {
        public Guid Id { get; set; }
        public Guid SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; }
        public Guid OpportunityId { get; set; }
        public Guid QuoteId { get; set; }
        public Guid SoldToContactId { get; set; }
        public string SoldToContactEmail { get; set; }
        public Guid SnowflakeOrderSplitId { get; set; }
        public Guid OwningBusinessUnitId { get; set; }
        public int IsSend { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public string ErrorMessage { get; set; }
    }
}
