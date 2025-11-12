using System;

namespace MunicipalServicesApp
{
    public class IssueReport
    {
        public string Reference { get; set; }                 // e.g., MUN-20250910-0001
        public string Location { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        // Custom list for file paths (JPG/PNG/PDF, etc.)
        public SimpleLinkedList<string> Attachments { get; set; } = new SimpleLinkedList<string>();

        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

        // ===== Dependency modelling (per lecturer requirement) =====
        // True if this is a root infrastructure issue (e.g., main water pipe burst).
        public bool IsMainIssue { get; set; }

        // If this is a dependent request, the reference of its main/root issue.
        // Example: for a "no water" complaint, ParentReference points to the main burst pipe ref.
        public string ParentReference { get; set; }
    }
}
