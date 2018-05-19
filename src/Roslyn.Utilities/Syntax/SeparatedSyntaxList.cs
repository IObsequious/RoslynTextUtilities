
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{

    public struct SeparatedSyntaxList<TNode> : IReadOnlyList<TNode>, IEquatable<SeparatedSyntaxList<TNode>> where TNode : SyntaxNode
    {
        private readonly SyntaxList<SyntaxNode> _list;

        public SeparatedSyntaxList(SyntaxList<SyntaxNode> list)
        {
            Validate(list);
            _list = list;
        }

        [Conditional("DEBUG")]
        private static void Validate(SyntaxList<SyntaxNode> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if ((i & 1) == 0)
                {
                    Debug.Assert(!item.IsToken, "even elements of a separated list must be nodes");
                }
                else
                {
                    Debug.Assert(item.IsToken, "odd elements of a separated list must be tokens");
                }
            }
        }

        public SyntaxNode Node => _list.Node;

        public int Count
        {
            get
            {
                return (_list.Count + 1) >> 1;
            }
        }

        public int SeparatorCount
        {
            get
            {
                return _list.Count >> 1;
            }
        }
        public TNode this[int index]
        {
            get
            {
                return (TNode)_list[index << 1];
            }
        }

        public SyntaxNode GetSeparator(int index)
        {
            return _list[(index << 1) + 1];
        }

        public SyntaxList<SyntaxNode> GetWithSeparators()
        {
            return _list;
        }
        public static bool operator ==(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SeparatedSyntaxList<TNode> left, SeparatedSyntaxList<TNode> right)
        {
            return !left.Equals(right);
        }

        public bool Equals(SeparatedSyntaxList<TNode> other)
        {
            return _list == other._list;
        }

        public override bool Equals(object obj)
        {
            return (obj is SeparatedSyntaxList<TNode>) && Equals((SeparatedSyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _list.GetHashCode();
        }
        public static implicit operator SeparatedSyntaxList<SyntaxNode>(SeparatedSyntaxList<TNode> list)
        {
            return new SeparatedSyntaxList<SyntaxNode>(list.GetWithSeparators());
        }
#if DEBUG
        [Obsolete("For debugging only", true)]
        private TNode[] Nodes
        {
            get
            {
                int count = Count;
                TNode[] array = new TNode[count];
                for (int i = 0; i < count; i++)
                {
                    array[i] = this[i];
                }
                return array;
            }
        }
#endif


        public Reversed Reverse() => new Reversed(this);

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator() => new EnumeratorImpl(this);

        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorImpl(this);

        public struct Enumerator
        {
            private readonly SeparatedSyntaxList<TNode> _list;
            private int _index;

            internal Enumerator(SeparatedSyntaxList<TNode> list)
            {
                _list = list;
                _index = -1;
            }

            public bool MoveNext()
            {
                int newIndex = _index + 1;
                if (newIndex < _list.Count)
                {
                    _index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return (TNode)_list[_index];
                }
            }

            public void Reset()
            {
                _index = -1;
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }

        private class EnumeratorImpl : IEnumerator<TNode>
        {
            private Enumerator _e;

            internal EnumeratorImpl(SeparatedSyntaxList<TNode>  list)
            {
                _e = new Enumerator(list);
            }

            public bool MoveNext()
            {
                return _e.MoveNext();
            }

            public TNode Current
            {
                get
                {
                    return _e.Current;
                }
            }

            void IDisposable.Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return _e.Current;
                }
            }

            void IEnumerator.Reset()
            {
                _e.Reset();
            }
        }

        public struct Reversed : IEnumerable<TNode>, IEquatable<Reversed>
        {
            private readonly SeparatedSyntaxList<TNode>  _collection;
            private readonly int _count;

            internal Reversed(SeparatedSyntaxList<TNode>  collection)
            {
                _collection = collection;
                _count = collection.Count;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_collection);
            }

            IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
            {
                if (_collection == null)
                {
                    return new List<TNode>.Enumerator();
                }

                return new EnumeratorImpl(_collection);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                if (_collection == null)
                {
                    return new List<TNode>.Enumerator();
                }

                return new EnumeratorImpl(_collection);
            }

            public bool Equals(Reversed other)
            {
                return _collection == other._collection
                    && _count == other._count;
            }

            public struct Enumerator
            {
                private readonly SeparatedSyntaxList<TNode>  _collection;
                private readonly int _count;
                private int _index;

                internal Enumerator(SeparatedSyntaxList<TNode>  collection)
                {
                    _collection = collection;
                    _count = _collection.Count;
                    _index = _count;
                }

                public bool MoveNext()
                {
                    return --_index >= 0;
                }

                public TNode Current
                {
                    get
                    {
                        return _collection[_index];
                    }
                }

                public void Reset()
                {
                    _index = _count;
                }
            }

            private class EnumeratorImpl : IEnumerator<TNode>
            {
                private Enumerator _enumerator;

                internal EnumeratorImpl(SeparatedSyntaxList<TNode>  collection)
                {
                    _enumerator = new Enumerator(collection);
                }

                public TNode Current
                {
                    get { return _enumerator.Current; }
                }

                object IEnumerator.Current
                {
                    get { return _enumerator.Current; }
                }

                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    _enumerator.Reset();
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
