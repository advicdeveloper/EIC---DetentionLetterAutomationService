using System;

namespace DetentionLetterFunctionApp.Models
{
    public class OrderSummary
    {
        public Guid SummaryId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string OrderName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string SoldToEmail { get; set; }
        public string DocumentPath { get; set; }
        public Guid OrderModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsProcessed { get; set; }
        public string Status { get; set; }
    }
}
