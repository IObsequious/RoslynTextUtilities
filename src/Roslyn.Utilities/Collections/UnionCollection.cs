using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class UnionCollection<T> : ICollection<T>
    {
        private readonly ImmutableArray<ICollection<T>> _collections;
        private int _count = -1;

        public static ICollection<T> Create(ICollection<T> coll1, ICollection<T> coll2)
        {
            Debug.Assert(coll1.IsReadOnly && coll2.IsReadOnly);
            if (coll1.Count == 0)
            {
                return coll2;
            }

            if (coll2.Count == 0)
            {
                return coll1;
            }

            return new UnionCollection<T>(ImmutableArray.Create(coll1, coll2));
        }

        public static ICollection<T> Create<TOrig>(ImmutableArray<TOrig> collections, Func<TOrig, ICollection<T>> selector)
        {
            Debug.Assert(collections.All(c => selector(c).IsReadOnly));
            switch (collections.Length)
            {
                case 0:
                    return SpecializedCollections.EmptyCollection<T>();
                case 1:
                    return selector(collections[0]);
                default:
                    return new UnionCollection<T>(ImmutableArray.CreateRange(collections, selector));
            }
        }

        private UnionCollection(ImmutableArray<ICollection<T>> collections)
        {
            Debug.Assert(!collections.IsDefault);
            _collections = collections;
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            foreach (var c in _collections)
            {
                if (c.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int index = arrayIndex;
            foreach (var collection in _collections)
            {
                collection.CopyTo(array, index);
                index += collection.Count;
            }
        }

        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    _count = _collections.Sum(c => c.Count);
                }

                return _count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collections.SelectMany(c => c).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
