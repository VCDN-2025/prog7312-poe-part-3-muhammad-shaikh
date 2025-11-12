using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp.Domain
{
    // Tracks user searches: categories, date-buckets, and keywords from the search box.
    public class SearchTracker
    {
        private readonly Dictionary<EventCategory, int> _categoryHits = new Dictionary<EventCategory, int>();
        private readonly Dictionary<int, int> _monthHits = new Dictionary<int, int>(); // key = YYYYMM
        private readonly Dictionary<string, int> _keywordHits = new Dictionary<string, int>(); // lowercased tokens

        // very small stoplist to avoid noise
        private static readonly HashSet<string> _stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "the","and","for","with","a","an","of","to","in","on","at","by" };

        public void Record(EventCategory? category, DateTime? date, string searchText)
        {
            // 1) record explicit category if provided
            if (category.HasValue)
            {
                if (!_categoryHits.ContainsKey(category.Value)) _categoryHits[category.Value] = 0;
                _categoryHits[category.Value]++;
            }

            // 2) record date bucket
            if (date.HasValue)
            {
                int bucket = date.Value.Year * 100 + date.Value.Month;
                if (!_monthHits.ContainsKey(bucket)) _monthHits[bucket] = 0;
                _monthHits[bucket]++;
            }

            // 3) tokenize keywords (as you already had)
            string s = (searchText ?? "").Trim().ToLowerInvariant();
            if (s.Length > 0)
            {
                foreach (var raw in s.Split(new[] { ' ', ',', '.', ';', ':', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var tok = raw.Trim();
                    if (tok.Length < 3) continue;
                    if (_stop.Contains(tok)) continue;

                    if (!_keywordHits.ContainsKey(tok)) _keywordHits[tok] = 0;
                    _keywordHits[tok]++;
                }
            }

            // 4) NEW: if no explicit category, infer one from search text and boost it
            if (!category.HasValue && !string.IsNullOrWhiteSpace(searchText))
            {
                var inferred = EventStore.InferCategoryFromText(searchText);
                if (inferred.HasValue)
                {
                    if (!_categoryHits.ContainsKey(inferred.Value)) _categoryHits[inferred.Value] = 0;
                    _categoryHits[inferred.Value] += 2; // give a stronger signal to make it “top”
                }
            }
        }


        public IEnumerable<EventCategory> TopCategories(int count = 2) =>
            _categoryHits.OrderByDescending(kv => kv.Value).Take(count).Select(kv => kv.Key);

        public IEnumerable<int> TopMonthBuckets(int count = 2) =>
            _monthHits.OrderByDescending(kv => kv.Value).Take(count).Select(kv => kv.Key);

        public IEnumerable<string> TopKeywords(int count = 4) =>
            _keywordHits.OrderByDescending(kv => kv.Value).Take(count).Select(kv => kv.Key);
    }
}
