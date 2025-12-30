using System;

namespace DetentionLetterAzureFunction.Models
{
    public class SalesOrder
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
        public Guid OpportunityId { get; set; }
        public Guid QuoteId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid OwningBusinessUnit { get; set; }
        public string ZipCode { get; set; }
    }
}
