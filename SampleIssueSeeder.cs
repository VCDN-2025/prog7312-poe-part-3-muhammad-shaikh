using System;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Seeds demo service requests into IssueRepository so that
    /// ServiceStatusForm can show data for your demo video.
    /// Includes dependency clusters (main issue + dependent requests).
    /// </summary>
    public static class SampleIssueSeeder
    {
        private static bool _seeded = false;

        public static void Seed()
        {
            // Only seed once and only if there is no data yet
            if (_seeded) return;
            if (IssueRepository.Count > 0) return;
            _seeded = true;

            // ========= Cluster A: Durban CBD (general items) =========
            Add(
                category: "Sanitation",
                location: "123 Florence Nzama St, Durban CBD",
                description: "Overflowing communal bin next to taxi rank. Smell and litter affecting pedestrians.",
                daysAgo: -12,
                status: "Received");

            Add(
                category: "Sanitation",
                location: "Smith St, Durban CBD",
                description: "Illegal dumping of construction rubble on sidewalk near bus stop.",
                daysAgo: -9,
                status: "In Progress");

            Add(
                category: "Roads & Stormwater",
                location: "Dr Pixley kaSeme St, Durban CBD",
                description: "Large pothole in right-hand lane causing vehicles to swerve into oncoming traffic.",
                daysAgo: -15,
                status: "In Progress");

            // ========= Cluster B: KwaMashu (roads) =========
            Add(
                category: "Roads & Stormwater",
                location: "M25, KwaMashu",
                description: "Blocked stormwater drain leading to flooding during heavy rain.",
                daysAgo: -20,
                status: "Received");

            Add(
                category: "Roads & Stormwater",
                location: "Princess Magogo Stadium, KwaMashu",
                description: "Street lights out along access road to stadium, poor visibility at night.",
                daysAgo: -6,
                status: "In Progress");

            // ========= Cluster C (DEPENDENCY): Phoenix Water main burst =========
            // Main/root issue
            var mainWater = Add(
                category: "Water & Utilities",
                location: "Phoenix Highway, Phoenix",
                description: "MAIN ISSUE: Bulk trunk main burst near Phoenix Highway. Crews dispatched.",
                daysAgo: -5,
                status: "In Progress",
                isMainIssue: true);

            // Dependents (household complaints relying on the main water issue)
            Add(
                category: "Water & Utilities",
                location: "Trenance Park, Phoenix",
                description: "No water supply since early morning. Multiple households affected.",
                daysAgo: -3,
                status: "Received",
                isMainIssue: false,
                parentRef: mainWater.Reference);

            Add(
                category: "Water & Utilities",
                location: "Phoenix Industrial Area",
                description: "Low water pressure across several factories on Phoenix Industrial.",
                daysAgo: -3,
                status: "Received",
                isMainIssue: false,
                parentRef: mainWater.Reference);

            // ========= Cluster D (DEPENDENCY): Electricity substation fault =========
            // Main/root issue (already resolved)
            var mainPower = Add(
                category: "Electricity",
                location: "Longcroft Substation, Phoenix",
                description: "MAIN ISSUE: Substation feeder fault affecting Longcroft & surrounds.",
                daysAgo: -2,
                status: "Resolved",          // main fixed
                isMainIssue: true);

            // Dependents (should auto-close because parent is resolved)
            Add(
                category: "Electricity",
                location: "Longcroft, Phoenix",
                description: "Power outage since storm. Neighbouring street restored, ours still down.",
                daysAgo: -2,
                status: "In Progress",
                isMainIssue: false,
                parentRef: mainPower.Reference);

            Add(
                category: "Electricity",
                location: "Woodview, Phoenix",
                description: "Intermittent power dips after the storm.",
                daysAgo: -1,
                status: "Received",
                isMainIssue: false,
                parentRef: mainPower.Reference);

            // ========= Cluster E: uMlazi (safety + electricity) =========
            Add(
                category: "Electricity",
                location: "uMlazi V Section, uMlazi",
                description: "Exposed live cable hanging low over pedestrian walkway.",
                daysAgo: -1,
                status: "Received");

            Add(
                category: "Safety & Security",
                location: "uMlazi Mega City, uMlazi",
                description: "Non-functioning street lights in parking area leading to safety concerns.",
                daysAgo: -8,
                status: "Received");

            // ========= Cluster F: Parks / Housing / Other =========
            Add(
                category: "Parks & Recreation",
                location: "People's Park, Moses Mabhida, Durban",
                description: "Play equipment damaged and unsafe for children, missing bolts on swings.",
                daysAgo: -7,
                status: "In Progress");

            Add(
                category: "Housing",
                location: "Cornubia Housing Project, Cornubia",
                description: "Roof leaks reported in several RDP units during heavy rainfall.",
                daysAgo: -30,
                status: "Received");

            Add(
                category: "Housing",
                location: "Cornubia Housing Project, Cornubia",
                description: "Blocked internal drains causing greywater to overflow into courtyards.",
                daysAgo: -18,
                status: "In Progress");

            Add(
                category: "Other",
                location: "Online, Citywide",
                description: "General query about refuse collection calendar for upcoming public holidays.",
                daysAgo: -4,
                status: "Resolved");

            Add(
                category: "Safety & Security",
                location: "City Hall, Durban CBD",
                description: "Request for improved security presence during evening events.",
                daysAgo: -11,
                status: "Received");

            // ========= Auto-cascade: if a main issue is Resolved, close its dependents =========
            CascadeResolvedParents();
        }

        /// <summary>
        /// Adds a new IssueReport with optional dependency flags.
        /// </summary>
        private static IssueReport Add(
            string category,
            string location,
            string description,
            int daysAgo,
            string status,
            bool isMainIssue = false,
            string parentRef = null)
        {
            var report = new IssueReport
            {
                Reference = ReferenceGenerator.Next(),
                Location = location,
                Category = category,
                Description = description,
                CreatedAt = DateTime.Now.AddDays(daysAgo),
                Status = status,
                IsMainIssue = isMainIssue,
                ParentReference = parentRef
            };

            // Empty attachment list using custom linked list
            report.Attachments = new SimpleLinkedList<string>();

            IssueRepository.Add(report);
            return report;
        }

        /// <summary>
        /// Walks the repository: whenever a main issue is Resolved,
        /// mark all dependents (ParentReference == main.Reference) as Resolved too.
        /// </summary>
        private static void CascadeResolvedParents()
        {
            // First collect all resolved parents
            var resolvedParents = new SimpleLinkedList<string>();
            IssueRepository.ForEach(r =>
            {
                if (r.IsMainIssue && string.Equals(r.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
                    resolvedParents.Add(r.Reference);
            });

            // For each resolved parent, close its dependents
            resolvedParents.ForEach(parentRef =>
            {
                IssueRepository.ForEach(child =>
                {
                    if (!child.IsMainIssue &&
                        !string.IsNullOrWhiteSpace(child.ParentReference) &&
                        string.Equals(child.ParentReference, parentRef, StringComparison.OrdinalIgnoreCase))
                    {
                        child.Status = "Resolved";
                    }
                });
            });
        }
    }
}
