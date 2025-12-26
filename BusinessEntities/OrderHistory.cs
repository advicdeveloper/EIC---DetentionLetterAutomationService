using System;

namespace CONTECH.Service.BusinessEntities
{
    public class OrderHistory
    {
        public Guid HistoryId { get; set; }
        public Guid SummaryId { get; set; }
        public string OrderNumber { get; set; }
        public string ErrorMessage { get; set; }
        public string ReportName { get; set; }
        public DateTime SendDate { get; set; }
        public bool IsSend { get; set; }
        public string FromAddress { get; set; }
        public string SentToAddress { get; set; }
        public Guid AssociatedToOrderId { get; set; }
        public string AttachmentPath { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid OwningBusinessUnit { get; set; }
        public double Size { get; set; }
        public string FileName { get; set; }
    }
}
