using System;

namespace MunicipalServicesApp
{
    // Minimal custom singly-linked list 
    public class SimpleLinkedList<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
            public Node(T value) { Value = value; }
        }

        private Node _head;
        private Node _tail;
        private int _count;

        public int Count { get { return _count; } }

        public void Add(T item)
        {
            var n = new Node(item);
            if (_head == null) { _head = _tail = n; }
            else { _tail.Next = n; _tail = n; }
            _count++;
        }

        // Remove first item matching predicate; returns true if removed
        public bool RemoveWhere(Func<T, bool> predicate)
        {
            Node prev = null, cur = _head;
            while (cur != null)
            {
                if (predicate(cur.Value))
                {
                    if (prev == null) _head = cur.Next; else prev.Next = cur.Next;
                    if (cur == _tail) _tail = prev;
                    _count--;
                    return true;
                }
                prev = cur; cur = cur.Next;
            }
            return false;
        }

        public bool Contains(T item)
        {
            Node cur = _head;
            while (cur != null)
            {
                if (object.Equals(cur.Value, item)) return true;
                cur = cur.Next;
            }
            return false;
        }

        public void Clear()
        {
            _head = _tail = null;
            _count = 0;
        }

        public void ForEach(Action<T> action)
        {
            Node cur = _head;
            while (cur != null)
            {
                action(cur.Value);
                cur = cur.Next;
            }
        }

        // Returns true if any node satisfies the predicate
        public bool Any(Func<T, bool> predicate)
        {
            Node cur = _head;
            while (cur != null)
            {
                if (predicate(cur.Value)) return true;
                cur = cur.Next;
            }
            return false;
        }
    }
}
