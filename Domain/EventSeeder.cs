using System;

namespace MunicipalServicesApp.Domain
{
    public static class EventSeeder
    {
        private static int _seq = 1;
        private static string NextId() => $"EVT-2025-{_seq++.ToString("D4")}";

        public static void Seed()
        {
            EventStore.Clear();

            Add("Water Interruption Notice - Ward 10", EventCategory.Utilities, 2025, 10, 20, 9, "Ward 10 Reservoir",
                "Scheduled maintenance. Expect outages from 09:00 to 14:00.");
            Add("Community Clean-up - Beachfront", EventCategory.Environment, 2025, 10, 22, 8, "North Beach",
                "Bring gloves and bags. Join to keep our coast clean.");
            Add("Council Meeting - Public Gallery", EventCategory.Council, 2025, 10, 25, 10, "City Hall Chamber",
                "Agenda: service backlog, budget adjustments, safety.");
            Add("Flu Vaccination Drive", EventCategory.Health, 2025, 10, 18, 9, "Ward 3 Clinic",
                "Free vaccinations. ID required.");
            Add("Traffic Calming Workshop", EventCategory.Transport, 2025, 10, 28, 14, "Transport Hub",
                "Discuss speed humps & safe crossings.");
            Add("Community Soccer Cup", EventCategory.Sports, 2025, 11, 2, 11, "Central Sports Grounds",
                "U16, U19 and open leagues.");
            Add("Book Fair - Local Authors", EventCategory.Culture, 2025, 11, 5, 10, "Civic Centre",
                "Meet authors, readings, signings.");
            Add("Load-shedding Advisory - Zone B", EventCategory.Utilities, 2025, 10, 17, 18, "Zone B",
                "Stage 2 possible 18:00–22:00.");
            Add("Housing List Workshop", EventCategory.Housing, 2025, 10, 29, 9, "Housing Office",
                "Learn how to apply and status checks.");
            Add("Park Tree-Planting Day", EventCategory.Environment, 2025, 11, 7, 9, "Greenfield Park",
                "Family-friendly. Tools provided.");
            Add("Safety Awareness Talk", EventCategory.Safety, 2025, 10, 21, 16, "Community Hall",
                "Neighborhood watch & tips.");
            Add("After-school Tutoring Launch", EventCategory.Education, 2025, 10, 27, 15, "Library Hall",
                "Math & science focus.");
            Add("City Marathon - Route Briefing", EventCategory.Sports, 2025, 11, 9, 17, "Stadium",
                "Route changes & safety brief.");
            Add("Waste Collection Update", EventCategory.Utilities, 2025, 10, 19, 7, "City Wide",
                "New schedule in effect.");
            Add("Cultural Night Market", EventCategory.Culture, 2025, 10, 31, 18, "Market Square",
                "Food stalls, music, crafts.");

            // Example of queue ingestion (management task)
            EventStore.NewSubmissions.Enqueue(new Event
            {
                Id = NextId(),
                Title = "Community Choir Auditions",
                Category = EventCategory.Culture,
                StartDate = new DateTime(2025, 11, 12, 17, 0, 0),
                Location = "Music School",
                Description = "Open to all ages. Prepare a 2-minute piece."
            });

            while (EventStore.NewSubmissions.Count > 0)
                EventStore.Add(EventStore.NewSubmissions.Dequeue());
        }

        private static void Add(string title, EventCategory cat, int y, int m, int d, int h, string location, string desc)
        {
            EventStore.Add(new Event
            {
                Id = NextId(),
                Title = title,
                Category = cat,
                StartDate = new DateTime(y, m, d, h, 0, 0),
                Location = location,
                Description = desc
            });
        }
    }
}
