using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp.Domain
{
    public static class EventStore
    {
        private static readonly SortedDictionary<DateTime, List<Event>> _byDate =
            new SortedDictionary<DateTime, List<Event>>();

        private static readonly Dictionary<string, Event> _byId =
            new Dictionary<string, Event>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<EventCategory> _categories = new HashSet<EventCategory>();

        public static readonly Stack<Event> LastViewed = new Stack<Event>();
        public static readonly Queue<Event> NewSubmissions = new Queue<Event>();
        public static readonly SimplePriorityQueue<Event> UpcomingByDate = new SimplePriorityQueue<Event>();

        public static readonly SearchTracker Tracker = new SearchTracker();

        public static IEnumerable<EventCategory> Categories => _categories.OrderBy(c => c);
        public static int Count => _byId.Count;

        public static void Clear()
        {
            _byDate.Clear(); _byId.Clear(); _categories.Clear(); LastViewed.Clear();
            while (NewSubmissions.Count > 0) NewSubmissions.Dequeue();
            while (UpcomingByDate.Count > 0) UpcomingByDate.Dequeue();
        }

        public static void Add(Event e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.Id)) return;

            _byId[e.Id] = e;
            _categories.Add(e.Category);

            var d = e.StartDate.Date;
            if (!_byDate.ContainsKey(d)) _byDate[d] = new List<Event>();
            _byDate[d].Add(e);

            UpcomingByDate.Enqueue(e.StartDate, e);
        }

        public static IEnumerable<Event> All()
        {
            foreach (var pair in _byDate)
                foreach (var e in pair.Value)
                    yield return e;
        }

        public static IEnumerable<Event> Find(DateTime? from, DateTime? to, EventCategory? category, string text)
        {
            IEnumerable<KeyValuePair<DateTime, List<Event>>> range = _byDate;

            if (from.HasValue || to.HasValue)
            {
                DateTime start = from?.Date ?? DateTime.MinValue.Date;
                DateTime end = to?.Date ?? DateTime.MaxValue.Date;
                range = _byDate.Where(kv => kv.Key >= start && kv.Key <= end);
            }

            string needle = (text ?? "").Trim().ToLowerInvariant();

            foreach (var kv in range)
            {
                foreach (var e in kv.Value)
                {
                    if (category.HasValue && e.Category != category.Value) continue;
                    if (needle.Length > 0)
                    {
                        if (!((e.Title ?? "").ToLowerInvariant().Contains(needle) ||
                              (e.Description ?? "").ToLowerInvariant().Contains(needle) ||
                              (e.Location ?? "").ToLowerInvariant().Contains(needle)))
                            continue;
                    }
                    yield return e;
                }
            }
        }

        public static IEnumerable<Event> Sort(IEnumerable<Event> src, string sortBy)
        {
            switch ((sortBy ?? "").ToLowerInvariant())
            {
                case "category": return src.OrderBy(e => e.Category).ThenBy(e => e.StartDate).ThenBy(e => e.Title);
                case "name": return src.OrderBy(e => e.Title).ThenBy(e => e.StartDate);
                case "date":
                default: return src.OrderBy(e => e.StartDate).ThenBy(e => e.Title);
            }
        }

        // -------- Keyword → Category mapping & inference (for recommendations) --------
        private static readonly Dictionary<EventCategory, string[]> _catKeywords =
            new Dictionary<EventCategory, string[]>
        {
            { EventCategory.Utilities,   new[] { "water", "outage", "interruption", "waste", "refuse", "collection", "sewer", "sanitation", "utility", "utilities" } },
            { EventCategory.Electricity, new[] { "loadshedding", "load-shedding", "load", "power", "electricity", "blackout" } },
            { EventCategory.Transport,   new[] { "traffic", "road", "bus", "route", "calming", "speed", "humps", "transport" } },
            { EventCategory.Environment, new[] { "beach", "clean-up", "cleanup", "trees", "park", "environment" } },
            { EventCategory.Sports,      new[] { "soccer", "marathon", "sports", "cup", "match" } },
            { EventCategory.Culture,     new[] { "market", "authors", "book", "choir", "music", "cultural", "festival" } },
            { EventCategory.Health,      new[] { "flu", "vaccination", "clinic", "health", "wellness" } },
            { EventCategory.Council,     new[] { "council", "meeting", "chamber", "agenda" } },
            { EventCategory.Housing,     new[] { "housing", "list", "workshop", "rental" } },
            { EventCategory.Safety,      new[] { "safety", "awareness", "watch", "security" } },
            { EventCategory.Education,   new[] { "tutoring", "school", "library", "education", "training" } },
            { EventCategory.Community,   new[] { "community", "ward", "volunteer" } },
            { EventCategory.Other,       new[] { "notice", "announcement", "update" } }
        };

        public static EventCategory? InferCategoryFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var s = text.ToLowerInvariant();

            EventCategory? best = null;
            int bestHits = 0;

            foreach (var kv in _catKeywords)
            {
                int hits = 0;
                foreach (var k in kv.Value)
                {
                    if (s.Contains(k)) hits++;
                }
                if (hits > bestHits)
                {
                    bestHits = hits;
                    best = kv.Key;
                }
            }
            return bestHits > 0 ? best : (EventCategory?)null;
        }

        // ---------------- Recommendation logic ----------------
        public static IEnumerable<Event> Recommend(int max = 5)
        {
            // Prefer strongest category preference first (explicit or inferred)
            var topCat = Tracker.TopCategories(1).FirstOrDefault();

            if (!EqualityComparer<EventCategory>.Default.Equals(topCat, default(EventCategory)))
            {
                var catEvents = All()
                    .Where(e => e.StartDate >= DateTime.Now && e.Category.Equals(topCat))
                    .OrderBy(e => e.StartDate)
                    .Take(max)
                    .ToList();

                if (catEvents.Count > 0) return catEvents;
            }

            // Fallback: blended scoring (months/keywords) if no clear category yet
            var topMonths = new HashSet<int>(Tracker.TopMonthBuckets(2)); // YYYYMM
            var topKeys = new List<string>(Tracker.TopKeywords(4));
            var upcoming = All().Where(e => e.StartDate >= DateTime.Now);

            var scored = upcoming.Select(e =>
            {
                int score = 0;

                int bucket = e.StartDate.Year * 100 + e.StartDate.Month;
                if (topMonths.Contains(bucket)) score += 1;

                if (topKeys.Count > 0)
                {
                    string t = (e.Title ?? "").ToLowerInvariant();
                    string d = (e.Description ?? "").ToLowerInvariant();
                    string l = (e.Location ?? "").ToLowerInvariant();

                    int kw = 0;
                    foreach (var k in topKeys)
                    {
                        if (t.Contains(k) || d.Contains(k) || l.Contains(k))
                        {
                            kw++;
                            if (kw >= 3) break; // cap keyword boost
                        }
                    }
                    score += kw; // 0..3
                }

                return new { e, score };
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.e.StartDate)
            .Select(x => x.e)
            .Take(max);

            return scored;
        }
    }
}
