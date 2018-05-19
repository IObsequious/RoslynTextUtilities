using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    [DebuggerDisplay(value: "Count = {Count}")]
    public sealed class ConcurrentSet<T> : ICollection<T>
    {
        private const int DefaultConcurrencyLevel = 2;
        private const int DefaultCapacity = 31;
        private readonly ConcurrentDictionary<T, byte> _dictionary;

        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity);
        }

        public ConcurrentSet(IEqualityComparer<T> equalityComparer)
        {
            _dictionary = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity, equalityComparer);
        }

        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return _dictionary.IsEmpty;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Contains(T value)
        {
            return _dictionary.ContainsKey(value);
        }

        public bool Add(T value)
        {
            return _dictionary.TryAdd(value, 0);
        }

        public void AddRange(IEnumerable<T> values)
        {
            if (values != null)
            {
                foreach (T v in values)
                {
                    Add(v);
                }
            }
        }

        public bool Remove(T value)
        {
            return _dictionary.TryRemove(value, out byte b);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public struct KeyEnumerator
        {
            private readonly IEnumerator<KeyValuePair<T, byte>> _kvpEnumerator;

            internal KeyEnumerator(IEnumerable<KeyValuePair<T, byte>> data)
            {
                _kvpEnumerator = data.GetEnumerator();
            }

            public T Current
            {
                get
                {
                    return _kvpEnumerator.Current.Key;
                }
            }

            public bool MoveNext()
            {
                return _kvpEnumerator.MoveNext();
            }

            public void Reset()
            {
                _kvpEnumerator.Reset();
            }
        }

        public KeyEnumerator GetEnumerator()
        {
            return new KeyEnumerator(_dictionary);
        }

        private IEnumerator<T> GetEnumeratorImpl()
        {
            foreach (KeyValuePair<T, byte> kvp in _dictionary)
            {
                yield return kvp.Key;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}
