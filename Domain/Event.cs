using System;

namespace MunicipalServicesApp.Domain
{
    public enum EventCategory
    {
        Community, Council, Electricity, Utilities, Sports, Culture, Education,
        Health, Safety, Housing, Environment, Transport, Other
    }

    public class Event
    {
        public string Id { get; set; }            // e.g., EVT-2025-0001
        public string Title { get; set; }
        public EventCategory Category { get; set; }
        public DateTime StartDate { get; set; }   // date+time
        public string Location { get; set; }
        public string Description { get; set; }

        public override string ToString() =>
            $"{StartDate:yyyy-MM-dd HH:mm} — {Title} ({Category}) @ {Location}";
    }
}
