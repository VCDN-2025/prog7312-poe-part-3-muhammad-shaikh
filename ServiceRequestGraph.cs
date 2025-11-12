using System;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Undirected weighted graph built over IssueReport objects.
    /// - Vertex = service request
    /// - Edge = similarity (same category / same area)
    /// Used for:
    ///   * Graph traversal (BFS) to find related requests
    ///   * Minimum Spanning Tree (Prim) to build a minimal "network"
    /// </summary>
    public class ServiceRequestGraph
    {
        private readonly IssueReport[] _nodes;
        private readonly int[,] _weights; // 0 = no edge, >0 = cost

        private ServiceRequestGraph(IssueReport[] nodes, int[,] weights)
        {
            _nodes = nodes;
            _weights = weights;
        }

        public int Count
        {
            get { return _nodes == null ? 0 : _nodes.Length; }
        }

        public static ServiceRequestGraph BuildFromRepository()
        {
            IssueReport[] nodes = IssueRepository.ToArray();
            int n = nodes.Length;
            if (n == 0)
                return new ServiceRequestGraph(nodes, new int[0, 0]);

            int[,] weights = new int[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    int w = ComputeWeight(nodes[i], nodes[j]);
                    if (w > 0)
                    {
                        weights[i, j] = w;
                        weights[j, i] = w;
                    }
                }
            }

            return new ServiceRequestGraph(nodes, weights);
        }

        // Lower weight = "closer" / more similar
        private static int ComputeWeight(IssueReport a, IssueReport b)
        {
            if (a == null || b == null) return 0;

            int score = 0;

            if (!string.IsNullOrEmpty(a.Category) &&
                !string.IsNullOrEmpty(b.Category) &&
                a.Category == b.Category)
            {
                score += 1;
            }

            string areaA = NormalizeArea(a.Location);
            string areaB = NormalizeArea(b.Location);
            if (areaA.Length > 0 && areaA == areaB)
            {
                score += 1;
            }

            if (score == 0) return 0;

            // Convert similarity score (1 or 2) to a cost:
            // more similar → smaller cost
            return 3 - score; // score 2 => cost 1, score 1 => cost 2
        }

        private static string NormalizeArea(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return "";
            string s = location.Trim();
            int comma = s.IndexOf(',');
            if (comma >= 0) s = s.Substring(0, comma);
            return s.ToLowerInvariant();
        }

        private int IndexOfReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference) || _nodes == null) return -1;

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (string.Equals(_nodes[i].Reference, reference, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// BFS traversal starting from a reference.
        /// Returns up to 'max' related IssueReport objects
        /// (including the starting one at index 0 in the result).
        /// </summary>
        public IssueReport[] GetRelatedByReference(string reference, int max)
        {
            int start = IndexOfReference(reference);
            if (start < 0 || Count == 0) return new IssueReport[0];
            if (max <= 0) max = 5;

            bool[] visited = new bool[Count];
            int[] queue = new int[Count];
            int head = 0, tail = 0;

            SimpleLinkedList<IssueReport> results = new SimpleLinkedList<IssueReport>();

            visited[start] = true;
            queue[tail++] = start;

            while (head < tail && results.Count < max + 1) // +1 to include self
            {
                int v = queue[head++];
                IssueReport node = _nodes[v];
                results.Add(node);

                for (int u = 0; u < Count; u++)
                {
                    if (!visited[u] && _weights[v, u] > 0)
                    {
                        visited[u] = true;
                        queue[tail++] = u;
                    }
                }
            }

            IssueReport[] arr = new IssueReport[results.Count];
            int idx = 0;
            results.ForEach(r => { arr[idx++] = r; });
            return arr;
        }

        /// <summary>
        /// Prim's algorithm for Minimum Spanning Tree.
        /// Returns the IssueReports in (one possible) MST order.
        /// </summary>
        public IssueReport[] ComputeMstOrder()
        {
            int n = Count;
            if (n == 0) return new IssueReport[0];
            if (n == 1) return new IssueReport[] { _nodes[0] };

            bool[] inTree = new bool[n];
            int[] minEdge = new int[n];

            for (int i = 0; i < n; i++)
            {
                inTree[i] = false;
                minEdge[i] = int.MaxValue;
            }

            // start from vertex 0
            minEdge[0] = 0;

            for (int step = 0; step < n; step++)
            {
                int v = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!inTree[i] && (v == -1 || minEdge[i] < minEdge[v]))
                        v = i;
                }
                if (v == -1) break;
                inTree[v] = true;

                for (int u = 0; u < n; u++)
                {
                    int w = _weights[v, u];
                    if (w > 0 && !inTree[u] && w < minEdge[u])
                    {
                        minEdge[u] = w;
                    }
                }
            }

            IssueReport[] order = new IssueReport[n];
            int pos = 0;
            for (int i = 0; i < n; i++)
            {
                if (inTree[i])
                    order[pos++] = _nodes[i];
            }

            if (pos < n)
            {
                IssueReport[] trimmed = new IssueReport[pos];
                for (int i = 0; i < pos; i++) trimmed[i] = order[i];
                return trimmed;
            }

            return order;
        }
    }
}
