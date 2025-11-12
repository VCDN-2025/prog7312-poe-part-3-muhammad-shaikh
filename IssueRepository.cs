using System;

namespace MunicipalServicesApp
{
    public static class IssueRepository
    {
        private static readonly SimpleLinkedList<IssueReport> _reports =
            new SimpleLinkedList<IssueReport>();

        // BST index by reference
        private static readonly ServiceRequestTree _byReference =
            new ServiceRequestTree();

        public static int Count
        {
            get { return _reports.Count; }
        }

        public static ServiceRequestTree ByReference
        {
            get { return _byReference; }
        }

        public static void Add(IssueReport report)
        {
            if (report == null) throw new ArgumentNullException("report");

            _reports.Add(report);

            if (!string.IsNullOrWhiteSpace(report.Reference))
            {
                _byReference.Insert(report.Reference, report);
            }
        }

        public static void ForEach(Action<IssueReport> action)
        {
            if (action == null) return;
            _reports.ForEach(action);
        }

        // NEW: convert all requests to an array (used by graph)
        public static IssueReport[] ToArray()
        {
            IssueReport[] arr = new IssueReport[_reports.Count];
            int i = 0;
            _reports.ForEach(r =>
            {
                arr[i++] = r;
            });
            return arr;
        }
    }

    // Simple per-day reference generator: MUN-YYYYMMDD-#### (resets daily)
    public static class ReferenceGenerator
    {
        private static DateTime _lastDate = DateTime.Today;
        private static int _counter = 0;
        private static readonly object _sync = new object();

        public static string Next()
        {
            lock (_sync)
            {
                DateTime today = DateTime.Today;
                if (today != _lastDate)
                {
                    _lastDate = today;
                    _counter = 0;
                }
                _counter++;
                return string.Format("MUN-{0:yyyyMMdd}-{1:0000}", today, _counter);
            }
        }
    }
}
