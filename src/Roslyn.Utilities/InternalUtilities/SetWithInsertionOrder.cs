using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    public sealed class SetWithInsertionOrder<T> : IEnumerable<T>, IReadOnlySet<T>
    {
        private HashSet<T> _set = null;
        private ArrayBuilder<T> _elements = null;

        public bool Add(T value)
        {
            if (_set == null)
            {
                _set = new HashSet<T>();
                _elements = new ArrayBuilder<T>();
            }

            if (!_set.Add(value))
            {
                return false;
            }

            _elements.Add(value);
            return true;
        }

        public bool Insert(int index, T value)
        {
            if (_set == null)
            {
                if (index > 0)
                {
                    throw new IndexOutOfRangeException();
                }

                Add(value);
            }
            else
            {
                if (!_set.Add(value))
                {
                    return false;
                }

                try
                {
                    _elements.Insert(index, value);
                }
                catch
                {
                    _set.Remove(value);
                    throw;
                }
            }

            return true;
        }

        public bool Remove(T value)
        {
            if (!_set.Remove(value))
            {
                return false;
            }

            _elements.RemoveAt(_elements.IndexOf(value));
            return true;
        }

        public int Count
        {
            get
            {
                return _elements?.Count ?? 0;
            }
        }

        public bool Contains(T item)
        {
            return _set?.Contains(item) ?? false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _elements?.GetEnumerator() ?? SpecializedCollections.EmptyEnumerator<T>();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ImmutableArray<T> AsImmutable()
        {
            return _elements.ToImmutableArray();
        }

        public T this[int i]
        {
            get
            {
                return _elements[i];
            }
        }
    }
}
