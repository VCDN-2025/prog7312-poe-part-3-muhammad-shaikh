using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Domain
{
    public class SimplePriorityQueue<T>
    {
        private readonly List<(DateTime key, T value)> _heap = new List<(DateTime, T)>();
        public int Count => _heap.Count;

        public void Enqueue(DateTime key, T value)
        {
            _heap.Add((key, value));
            SiftUp(_heap.Count - 1);
        }

        public T Dequeue()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("Empty queue.");
            var root = _heap[0].value;
            var last = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            if (_heap.Count > 0) { _heap[0] = last; SiftDown(0); }
            return root;
        }

        public (DateTime key, T value) Peek()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("Empty queue.");
            return _heap[0];
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_heap[p].key <= _heap[i].key) break;
                (_heap[p], _heap[i]) = (_heap[i], _heap[p]);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            int n = _heap.Count;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, s = i;
                if (l < n && _heap[l].key < _heap[s].key) s = l;
                if (r < n && _heap[r].key < _heap[s].key) s = r;
                if (s == i) break;
                (_heap[s], _heap[i]) = (_heap[i], _heap[s]);
                i = s;
            }
        }
    }
}
