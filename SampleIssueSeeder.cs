using System;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Seeds some demo service requests into IssueRepository so that
    /// ServiceStatusForm can show data for your demo video.
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

            // Area: Durban CBD, category SANITATION & ROADS
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

            // Area: KwaMashu (roads cluster)
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

            // Area: Phoenix (water + electricity cluster)
            Add(
                category: "Water & Utilities",
                location: "Phoenix Highway, Phoenix",
                description: "Ongoing water leak in the middle of the road, constant stream for two weeks.",
                daysAgo: -5,
                status: "Received");

            Add(
                category: "Water & Utilities",
                location: "Trenance Park, Phoenix",
                description: "Low water pressure and intermittent supply reported by multiple households.",
                daysAgo: -3,
                status: "In Progress");

            Add(
                category: "Electricity",
                location: "Longcroft, Phoenix",
                description: "Multiple households without power after storm. Neighbouring street already restored.",
                daysAgo: -2,
                status: "In Progress");

            // Area: uMlazi (safety + electricity cluster)
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

            // Parks / recreation
            Add(
                category: "Parks & Recreation",
                location: "People's Park, Moses Mabhida, Durban",
                description: "Play equipment damaged and unsafe for children, missing bolts on swings.",
                daysAgo: -7,
                status: "In Progress");

            // Housing
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

            // Citywide / Other
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
        }

        private static void Add(string category, string location, string description, int daysAgo, string status)
        {
            var report = new IssueReport();
            report.Reference = ReferenceGenerator.Next();
            report.Location = location;
            report.Category = category;
            report.Description = description;
            report.CreatedAt = DateTime.Now.AddDays(daysAgo);
            report.Status = status;

            // Empty attachment list using custom linked list
            var atts = new SimpleLinkedList<string>();
            report.Attachments = atts;

            IssueRepository.Add(report);
        }
    }
}
