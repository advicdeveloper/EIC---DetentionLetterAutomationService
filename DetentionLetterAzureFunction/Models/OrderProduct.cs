using System;

namespace DetentionLetterAzureFunction.Models
{
    public class OrderProduct
    {
        public Guid Id { get; set; }
        public Guid SalesOrderId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductFamily { get; set; }
        public string PartNumber { get; set; }
        public decimal Quantity { get; set; }
    }
}
