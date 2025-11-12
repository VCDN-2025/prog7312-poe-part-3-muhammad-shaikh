using System;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Simple Binary Search Tree (BST) keyed by reference number.
    /// Used to organise and retrieve IssueReport objects efficiently.
    /// </summary>
    public class ServiceRequestTree
    {
        private class Node
        {
            public string Key;
            public IssueReport Value;
            public Node Left;
            public Node Right;

            public Node(string key, IssueReport value)
            {
                Key = key;
                Value = value;
            }
        }

        private Node _root;

        public void Insert(string key, IssueReport value)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
                return;

            _root = Insert(_root, key, value);
        }

        private Node Insert(Node node, string key, IssueReport value)
        {
            if (node == null)
                return new Node(key, value);

            int cmp = string.Compare(key, node.Key, StringComparison.OrdinalIgnoreCase);

            if (cmp < 0)
                node.Left = Insert(node.Left, key, value);
            else if (cmp > 0)
                node.Right = Insert(node.Right, key, value);
            else
                node.Value = value; // update existing

            return node;
        }

        /// <summary>
        /// Exact lookup by reference (O(h) time).
        /// Returns null if the key is not found.
        /// </summary>
        public IssueReport Find(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            Node current = _root;

            while (current != null)
            {
                int cmp = string.Compare(key, current.Key, StringComparison.OrdinalIgnoreCase);
                if (cmp == 0)
                    return current.Value;
                if (cmp < 0)
                    current = current.Left;
                else
                    current = current.Right;
            }

            return null;
        }
    }
}
