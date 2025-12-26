using System;

namespace CONTECH.Service.BusinessEntities
{
    public class OrderSummary
    {

        public Guid SummaryId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public Guid OpportunityId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectNumber { get; set; }
        public Guid QuoteId { get; set; }
        public string QuoteName { get; set; }
        public string QuoteNumber { get; set; }
        public Guid SoldToContactid { get; set; }
        public string SoldToContactName { get; set; }
        public string SoldToPhone { get; set; }
        public string SoldToEmail { get; set; }

        public string DocumentPath { get; set; }
        public Guid OwningBusinessUnit { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public bool IsSend { get; set; }

        public Guid OwnerId { get; set; }
        public Guid OrderModifiedBy { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime SendDate { get; set; }
        public string OrderName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Message { get; set; }
    }
}
