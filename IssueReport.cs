using System;

namespace MunicipalServicesApp
{
    public class IssueReport
    {
        public string Reference { get; set; }                 // e.g., MUN-20250910-0001
        public string Location { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public SimpleLinkedList<string> Attachments { get; set; }  // custom list
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
