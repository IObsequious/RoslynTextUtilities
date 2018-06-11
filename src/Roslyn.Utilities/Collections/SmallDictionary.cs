using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public sealed class SmallDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        private AvlNode _root;
        private readonly IEqualityComparer<K> _comparer;
        public static readonly SmallDictionary<K, V> Empty = new SmallDictionary<K, V>(null);

        public SmallDictionary() : this(EqualityComparer<K>.Default)
        {
        }

        public SmallDictionary(IEqualityComparer<K> comparer)
        {
            _comparer = comparer;
        }

        public SmallDictionary(SmallDictionary<K, V> other, IEqualityComparer<K> comparer)
            : this(comparer)
        {
            foreach (KeyValuePair<K, V> kv in other)
            {
                Add(kv.Key, kv.Value);
            }
        }

        private bool CompareKeys(K k1, K k2)
        {
            return _comparer.Equals(k1, k2);
        }

        private int GetHashCode(K k)
        {
            return _comparer.GetHashCode(k);
        }

        public bool TryGetValue(K key, out V value)
        {
            if (_root != null)
            {
                return TryGetValue(GetHashCode(key), key, out value);
            }

            value = default;
            return false;
        }

        public void Add(K key, V value)
        {
            Insert(GetHashCode(key), key, value, true);
        }

        public V this[K key]
        {
            get
            {
                V value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException($"Could not find key {key}");
                }

                return value;
            }
            set => Insert(GetHashCode(key), key, value, false);
        }

        public bool ContainsKey(K key)
        {
            V value;
            return TryGetValue(key, out value);
        }

        [Conditional("DEBUG")]
        public void AssertBalanced()
        {
#if DEBUG
            AvlNode.AssertBalanced(_root);
#endif
        }

        private abstract class Node
        {
            public readonly K Key;
            public V Value;

            protected Node(K key, V value)
            {
                Key = key;
                Value = value;
            }

            public virtual Node Next => null;
        }

        private sealed class NodeLinked : Node
        {
            public NodeLinked(K key, V value, Node next)
                : base(key, value)
            {
                Next = next;
            }

            public override Node Next { get; }
        }

        private sealed class AvlNodeHead : AvlNode
        {
            public Node next;

            public AvlNodeHead(int hashCode, K key, V value, Node next)
                : base(hashCode, key, value)
            {
                this.next = next;
            }

            public override Node Next => next;
        }

        private abstract class HashedNode : Node
        {
            public readonly int HashCode;
            public sbyte Balance;

            protected HashedNode(int hashCode, K key, V value)
                : base(key, value)
            {
                HashCode = hashCode;
            }
        }

        private class AvlNode : HashedNode
        {
            public AvlNode Left;
            public AvlNode Right;

            public AvlNode(int hashCode, K key, V value)
                : base(hashCode, key, value)
            {
            }
#if DEBUG
            public static int AssertBalanced(AvlNode V)
            {
                if (V == null) return 0;
                int a = AssertBalanced(V.Left);
                int b = AssertBalanced(V.Right);
                if (a - b != V.Balance || Math.Abs(a - b) >= 2)
                {
                    throw new InvalidOperationException();
                }

                return 1 + Math.Max(a, b);
            }
#endif
        }

        private bool TryGetValue(int hashCode, K key, out V value)
        {
            AvlNode b = _root;
            do
            {
                if (b.HashCode > hashCode)
                {
                    b = b.Left;
                }
                else if (b.HashCode < hashCode)
                {
                    b = b.Right;
                }
                else
                {
                    goto hasBucket;
                }
            }
            while (b != null);

            value = default;
            return false;
            hasBucket:
            if (CompareKeys(b.Key, key))
            {
                value = b.Value;
                return true;
            }

            return GetFromList(b.Next, key, out value);
        }

        private bool GetFromList(Node next, K key, out V value)
        {
            while (next != null)
            {
                if (CompareKeys(key, next.Key))
                {
                    value = next.Value;
                    return true;
                }

                next = next.Next;
            }

            value = default;
            return false;
        }

        private void Insert(int hashCode, K key, V value, bool add)
        {
            AvlNode currentNode = _root;
            if (currentNode == null)
            {
                _root = new AvlNode(hashCode, key, value);
                return;
            }

            AvlNode currentNodeParent = null;
            AvlNode unbalanced = currentNode;
            AvlNode unbalancedParent = null;
            for (;;)
            {
                int hc = currentNode.HashCode;
                if (currentNode.Balance != 0)
                {
                    unbalancedParent = currentNodeParent;
                    unbalanced = currentNode;
                }

                if (hc > hashCode)
                {
                    if (currentNode.Left == null)
                    {
                        currentNode.Left = currentNode = new AvlNode(hashCode, key, value);
                        break;
                    }

                    currentNodeParent = currentNode;
                    currentNode = currentNode.Left;
                }
                else if (hc < hashCode)
                {
                    if (currentNode.Right == null)
                    {
                        currentNode.Right = currentNode = new AvlNode(hashCode, key, value);
                        break;
                    }

                    currentNodeParent = currentNode;
                    currentNode = currentNode.Right;
                }
                else
                {
                    HandleInsert(currentNode, currentNodeParent, key, value, add);
                    return;
                }
            }

            Debug.Assert(unbalanced != currentNode);
            AvlNode n = unbalanced;
            do
            {
                Debug.Assert(n.HashCode != hashCode);
                if (n.HashCode < hashCode)
                {
                    n.Balance--;
                    n = n.Right;
                }
                else
                {
                    n.Balance++;
                    n = n.Left;
                }
            }
            while (n != currentNode);

            AvlNode rotated;
            sbyte balance = unbalanced.Balance;
            if (balance == -2)
            {
                rotated = unbalanced.Right.Balance < 0 ?
                    LeftSimple(unbalanced) :
                    LeftComplex(unbalanced);
            }
            else if (balance == 2)
            {
                rotated = unbalanced.Left.Balance > 0 ?
                    RightSimple(unbalanced) :
                    RightComplex(unbalanced);
            }
            else
            {
                return;
            }

            if (unbalancedParent == null)
            {
                _root = rotated;
            }
            else if (unbalanced == unbalancedParent.Left)
            {
                unbalancedParent.Left = rotated;
            }
            else
            {
                unbalancedParent.Right = rotated;
            }
        }

        private static AvlNode LeftSimple(AvlNode unbalanced)
        {
            AvlNode right = unbalanced.Right;
            unbalanced.Right = right.Left;
            right.Left = unbalanced;
            unbalanced.Balance = 0;
            right.Balance = 0;
            return right;
        }

        private static AvlNode RightSimple(AvlNode unbalanced)
        {
            AvlNode left = unbalanced.Left;
            unbalanced.Left = left.Right;
            left.Right = unbalanced;
            unbalanced.Balance = 0;
            left.Balance = 0;
            return left;
        }

        private static AvlNode LeftComplex(AvlNode unbalanced)
        {
            AvlNode right = unbalanced.Right;
            AvlNode rightLeft = right.Left;
            right.Left = rightLeft.Right;
            rightLeft.Right = right;
            unbalanced.Right = rightLeft.Left;
            rightLeft.Left = unbalanced;
            sbyte rightLeftBalance = rightLeft.Balance;
            rightLeft.Balance = 0;
            if (rightLeftBalance < 0)
            {
                right.Balance = 0;
                unbalanced.Balance = 1;
            }
            else
            {
                right.Balance = (sbyte) -rightLeftBalance;
                unbalanced.Balance = 0;
            }

            return rightLeft;
        }

        private static AvlNode RightComplex(AvlNode unbalanced)
        {
            AvlNode left = unbalanced.Left;
            AvlNode leftRight = left.Right;
            left.Right = leftRight.Left;
            leftRight.Left = left;
            unbalanced.Left = leftRight.Right;
            leftRight.Right = unbalanced;
            sbyte leftRightBalance = leftRight.Balance;
            leftRight.Balance = 0;
            if (leftRightBalance < 0)
            {
                left.Balance = 1;
                unbalanced.Balance = 0;
            }
            else
            {
                left.Balance = 0;
                unbalanced.Balance = (sbyte) -leftRightBalance;
            }

            return leftRight;
        }

        private void HandleInsert(AvlNode node, AvlNode parent, K key, V value, bool add)
        {
            Node currentNode = node;
            do
            {
                if (CompareKeys(currentNode.Key, key))
                {
                    if (add)
                    {
                        throw new InvalidOperationException();
                    }

                    currentNode.Value = value;
                    return;
                }

                currentNode = currentNode.Next;
            }
            while (currentNode != null);

            AddNode(node, parent, key, value);
        }

        private void AddNode(AvlNode node, AvlNode parent, K key, V value)
        {
            AvlNodeHead head = node as AvlNodeHead;
            if (head != null)
            {
                NodeLinked newNext = new NodeLinked(key, value, head.next);
                head.next = newNext;
                return;
            }

            AvlNodeHead newHead = new AvlNodeHead(node.HashCode, key, value, node);
            newHead.Balance = node.Balance;
            newHead.Left = node.Left;
            newHead.Right = node.Right;
            if (parent == null)
            {
                _root = newHead;
                return;
            }

            if (node == parent.Left)
            {
                parent.Left = newHead;
            }
            else
            {
                parent.Right = newHead;
            }
        }

        public KeyCollection Keys => new KeyCollection(this);

        public struct KeyCollection : IEnumerable<K>
        {
            private readonly SmallDictionary<K, V> _dict;

            public KeyCollection(SmallDictionary<K, V> dict)
            {
                _dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode> _stack;
                private Node _next;
                private Node _current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    AvlNode root = dict._root;
                    if (root != null)
                    {
                        if (root.Left == root.Right)
                        {
                            _next = dict._root;
                        }
                        else
                        {
                            _stack = new Stack<AvlNode>(dict.HeightApprox());
                            _stack.Push(dict._root);
                        }
                    }
                }

                public K Current => _current.Key;

                public bool MoveNext()
                {
                    if (_next != null)
                    {
                        _current = _next;
                        _next = _next.Next;
                        return true;
                    }

                    if (_stack == null || _stack.Count == 0)
                    {
                        return false;
                    }

                    AvlNode curr = _stack.Pop();
                    _current = curr;
                    _next = curr.Next;
                    PushIfNotNull(curr.Left);
                    PushIfNotNull(curr.Right);
                    return true;
                }

                private void PushIfNotNull(AvlNode child)
                {
                    if (child != null)
                    {
                        _stack.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dict);
            }

            public class EnumerableImpl : IEnumerator<K>
            {
                private Enumerator _e;

                public EnumerableImpl(Enumerator e)
                {
                    _e = e;
                }

                K IEnumerator<K>.Current => _e.Current;

                void IDisposable.Dispose()
                {
                }

                object IEnumerator.Current => _e.Current;

                bool IEnumerator.MoveNext()
                {
                    return _e.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    throw new NotSupportedException();
                }
            }

            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }

        public ValueCollection Values => new ValueCollection(this);

        public struct ValueCollection : IEnumerable<V>
        {
            private readonly SmallDictionary<K, V> _dict;

            public ValueCollection(SmallDictionary<K, V> dict)
            {
                _dict = dict;
            }

            public struct Enumerator
            {
                private readonly Stack<AvlNode> _stack;
                private Node _next;
                private Node _current;

                public Enumerator(SmallDictionary<K, V> dict)
                    : this()
                {
                    AvlNode root = dict._root;
                    if (root == null)
                    {
                        return;
                    }

                    if (root.Left == root.Right)
                    {
                        _next = dict._root;
                    }
                    else
                    {
                        _stack = new Stack<AvlNode>(dict.HeightApprox());
                        _stack.Push(dict._root);
                    }
                }

                public V Current => _current.Value;

                public bool MoveNext()
                {
                    if (_next != null)
                    {
                        _current = _next;
                        _next = _next.Next;
                        return true;
                    }

                    if (_stack == null || _stack.Count == 0)
                    {
                        return false;
                    }

                    AvlNode curr = _stack.Pop();
                    _current = curr;
                    _next = curr.Next;
                    PushIfNotNull(curr.Left);
                    PushIfNotNull(curr.Right);
                    return true;
                }

                private void PushIfNotNull(AvlNode child)
                {
                    if (child != null)
                    {
                        _stack.Push(child);
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_dict);
            }

            public class EnumerableImpl : IEnumerator<V>
            {
                private Enumerator _e;

                public EnumerableImpl(Enumerator e)
                {
                    _e = e;
                }

                V IEnumerator<V>.Current => _e.Current;

                void IDisposable.Dispose()
                {
                }

                object IEnumerator.Current => _e.Current;

                bool IEnumerator.MoveNext()
                {
                    return _e.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    throw new NotSupportedException();
                }
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return new EnumerableImpl(GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }

        public struct Enumerator
        {
            private readonly Stack<AvlNode> _stack;
            private Node _next;
            private Node _current;

            public Enumerator(SmallDictionary<K, V> dict)
                : this()
            {
                AvlNode root = dict._root;
                if (root == null)
                {
                    return;
                }

                if (root.Left == root.Right)
                {
                    _next = dict._root;
                }
                else
                {
                    _stack = new Stack<AvlNode>(dict.HeightApprox());
                    _stack.Push(dict._root);
                }
            }

            public KeyValuePair<K, V> Current => new KeyValuePair<K, V>(_current.Key, _current.Value);

            public bool MoveNext()
            {
                if (_next != null)
                {
                    _current = _next;
                    _next = _next.Next;
                    return true;
                }

                if (_stack == null || _stack.Count == 0)
                {
                    return false;
                }

                AvlNode curr = _stack.Pop();
                _current = curr;
                _next = curr.Next;
                PushIfNotNull(curr.Left);
                PushIfNotNull(curr.Right);
                return true;
            }

            private void PushIfNotNull(AvlNode child)
            {
                if (child != null)
                {
                    _stack.Push(child);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public class EnumerableImpl : IEnumerator<KeyValuePair<K, V>>
        {
            private Enumerator _e;

            public EnumerableImpl(Enumerator e)
            {
                _e = e;
            }

            KeyValuePair<K, V> IEnumerator<KeyValuePair<K, V>>.Current => _e.Current;

            void IDisposable.Dispose()
            {
            }

            object IEnumerator.Current => _e.Current;

            bool IEnumerator.MoveNext()
            {
                return _e.MoveNext();
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return new EnumerableImpl(GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        private int HeightApprox()
        {
            int h = 0;
            AvlNode cur = _root;
            while (cur != null)
            {
                h++;
                cur = cur.Left;
            }

            return h + (h / 2);
        }
    }
}
