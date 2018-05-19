

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{

    public partial struct SyntaxList<TNode> where TNode : SyntaxNode
    {


        public Reversed Reverse() => new Reversed(this);

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator() => new EnumeratorImpl(this);

        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorImpl(this);

        public struct Enumerator
        {
            private readonly SyntaxList<TNode> _list;
            private int _index;

            internal Enumerator(SyntaxList<TNode> list)
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

            internal EnumeratorImpl(SyntaxList<TNode> list)
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
            private readonly SyntaxList<TNode> _collection;
            private readonly int _count;

            internal Reversed(SyntaxList<TNode> collection)
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
                private readonly SyntaxList<TNode> _collection;
                private readonly int _count;
                private int _index;

                internal Enumerator(SyntaxList<TNode> collection)
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

                internal EnumeratorImpl(SyntaxList<TNode> collection)
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
