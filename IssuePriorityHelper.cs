using System;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Simple min-heap for IssueReport, ordered by CreatedAt (oldest first).
    /// Custom implementation to demonstrate heap usage (no List<T>).
    /// </summary>
    public class IssueMinHeap
    {
        private IssueReport[] _items;
        private int _count;

        public IssueMinHeap(int capacity = 32)
        {
            if (capacity < 1) capacity = 1;
            _items = new IssueReport[capacity];
            _count = 0;
        }

        public int Count
        {
            get { return _count; }
        }

        public bool IsEmpty
        {
            get { return _count == 0; }
        }

        public void Insert(IssueReport report)
        {
            if (report == null) return;

            EnsureCapacity();
            _items[_count] = report;
            HeapifyUp(_count);
            _count++;
        }

        public IssueReport Peek()
        {
            if (_count == 0) return null;
            return _items[0];
        }

        public IssueReport ExtractMin()
        {
            if (_count == 0) return null;

            IssueReport min = _items[0];
            _count--;

            if (_count > 0)
            {
                _items[0] = _items[_count];
                _items[_count] = null;
                HeapifyDown(0);
            }
            else
            {
                _items[0] = null;
            }

            return min;
        }

        private void EnsureCapacity()
        {
            if (_count < _items.Length) return;

            int newCap = _items.Length * 2;
            if (newCap < 1) newCap = 1;

            var tmp = new IssueReport[newCap];
            for (int i = 0; i < _items.Length; i++)
                tmp[i] = _items[i];

            _items = tmp;
        }

        private static bool Older(IssueReport a, IssueReport b)
        {
            if (a == null) return false;
            if (b == null) return true;
            return a.CreatedAt < b.CreatedAt;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Older(_items[index], _items[parent]))
                {
                    Swap(index, parent);
                    index = parent;
                }
                else break;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < _count && Older(_items[left], _items[smallest]))
                    smallest = left;
                if (right < _count && Older(_items[right], _items[smallest]))
                    smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            IssueReport tmp = _items[i];
            _items[i] = _items[j];
            _items[j] = tmp;
        }
    }

    /// <summary>
    /// Helper for using the heap against the existing IssueRepository.
    /// Builds a heap from all requests and returns the N oldest.
    /// </summary>
    public static class IssuePriorityHelper
    {
        public static IssueReport[] GetOldest(int max)
        {
            if (max <= 0) return new IssueReport[0];

            IssueMinHeap heap = new IssueMinHeap();

            // Fill heap from repository
            IssueRepository.ForEach(report =>
            {
                heap.Insert(report);
            });

            int take = heap.Count < max ? heap.Count : max;
            IssueReport[] result = new IssueReport[take];

            for (int i = 0; i < take; i++)
            {
                result[i] = heap.ExtractMin(); // oldest first
            }

            return result;
        }
    }
}
