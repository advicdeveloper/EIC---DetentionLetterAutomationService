using System;

namespace DetentionLetterFunctionApp.Models
{
    public class OrderHistory
    {
        public Guid HistoryId { get; set; }
        public Guid SummaryId { get; set; }
        public Guid AssociatedToOrderId { get; set; }
        public string ReportName { get; set; }
        public string AttachmentPath { get; set; }
        public bool IsGenerated { get; set; }
        public double FileSize { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
