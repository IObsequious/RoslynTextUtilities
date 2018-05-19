using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Collections
{
    public sealed class OrderPreservingMultiDictionary<K, V> :
        IEnumerable<KeyValuePair<K, OrderPreservingMultiDictionary<K, V>.ValueSet>>
    {
        #region Pooling

        private readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> _pool;

        private OrderPreservingMultiDictionary(ObjectPool<OrderPreservingMultiDictionary<K, V>> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            if (_dictionary != null)
            {
                foreach (KeyValuePair<K, ValueSet> kvp in _dictionary)
                {
                    kvp.Value.Free();
                }

                _dictionary.Free();
                _dictionary = null;
            }

            _pool?.Free(this);
        }

        private static readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> s_poolInstance = CreatePool();

        public static ObjectPool<OrderPreservingMultiDictionary<K, V>> CreatePool()
        {
            ObjectPool<OrderPreservingMultiDictionary<K, V>> pool = null;
            pool = new ObjectPool<OrderPreservingMultiDictionary<K, V>>(factory: () => new OrderPreservingMultiDictionary<K, V>(pool),
                size: 16);
            return pool;
        }

        public static OrderPreservingMultiDictionary<K, V> GetInstance()
        {
            OrderPreservingMultiDictionary<K, V> instance = s_poolInstance.Allocate();
            Debug.Assert(instance.IsEmpty);
            return instance;
        }

        #endregion Pooling

        private static readonly Dictionary<K, ValueSet> s_emptyDictionary = new Dictionary<K, ValueSet>();
        private PooledDictionary<K, ValueSet> _dictionary;

        public OrderPreservingMultiDictionary()
        {
        }

        private void EnsureDictionary()
        {
            _dictionary = _dictionary ?? PooledDictionary<K, ValueSet>.GetInstance();
        }

        public bool IsEmpty
        {
            get
            {
                return _dictionary == null;
            }
        }

        public void Add(K k, V v)
        {
            if (!IsEmpty && _dictionary.TryGetValue(k, out ValueSet valueSet))
            {
                Debug.Assert(valueSet.Count >= 1);
                _dictionary[k] = valueSet.WithAddedItem(v);
            }
            else
            {
                EnsureDictionary();
                _dictionary[k] = new ValueSet(v);
            }
        }

        public Dictionary<K, ValueSet>.Enumerator GetEnumerator()
        {
            return IsEmpty ? s_emptyDictionary.GetEnumerator() : _dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<K, ValueSet>> IEnumerable<KeyValuePair<K, ValueSet>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ImmutableArray<V> this[K k]
        {
            get
            {
                if (!IsEmpty && _dictionary.TryGetValue(k, out ValueSet valueSet))
                {
                    Debug.Assert(valueSet.Count >= 1);
                    return valueSet.Items;
                }

                return ImmutableArray<V>.Empty;
            }
        }

        public bool Contains(K key, V value)
        {
            return !IsEmpty &&
                   _dictionary.TryGetValue(key, out ValueSet valueSet) &&
                   valueSet.Contains(value);
        }

        public Dictionary<K, ValueSet>.KeyCollection Keys
        {
            get
            {
                return IsEmpty ? s_emptyDictionary.Keys : _dictionary.Keys;
            }
        }

        public struct ValueSet : IEnumerable<V>
        {
            private readonly object _value;

            internal ValueSet(V value)
            {
                _value = value;
            }

            internal ValueSet(ArrayBuilder<V> values)
            {
                _value = values;
            }

            public void Free()
            {
                ArrayBuilder<V> arrayBuilder = _value as ArrayBuilder<V>;
                arrayBuilder?.Free();
            }

            internal V this[int index]
            {
                get
                {
                    Debug.Assert(Count >= 1);
                    ArrayBuilder<V> arrayBuilder = _value as ArrayBuilder<V>;
                    if (arrayBuilder == null)
                    {
                        if (index == 0)
                        {
                            return (V) _value;
                        }

                        throw new IndexOutOfRangeException();
                    }

                    return arrayBuilder[index];
                }
            }

            public bool Contains(V item)
            {
                Debug.Assert(Count >= 1);
                ArrayBuilder<V> arrayBuilder = _value as ArrayBuilder<V>;
                return arrayBuilder == null ? EqualityComparer<V>.Default.Equals(item, (V) _value) : arrayBuilder.Contains(item);
            }

            internal ImmutableArray<V> Items
            {
                get
                {
                    Debug.Assert(Count >= 1);
                    ArrayBuilder<V> arrayBuilder = _value as ArrayBuilder<V>;
                    if (arrayBuilder == null)
                    {
                        Debug.Assert(_value is V, message: "Item must be a a V");
                        return ImmutableArray.Create<V>((V) _value);
                    }

                    return arrayBuilder.ToImmutable();
                }
            }

            internal int Count
            {
                get
                {
                    return (_value as ArrayBuilder<V>)?.Count ?? 1;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            public ValueSet WithAddedItem(V item)
            {
                Debug.Assert(Count >= 1);
                ArrayBuilder<V> arrayBuilder = _value as ArrayBuilder<V>;
                if (arrayBuilder == null)
                {
                    Debug.Assert(_value is V, message: "_value must be a V");
                    arrayBuilder = ArrayBuilder<V>.GetInstance(2);
                    arrayBuilder.Add((V) _value);
                    arrayBuilder.Add(item);
                }
                else
                {
                    arrayBuilder.Add(item);
                }

                return new ValueSet(arrayBuilder);
            }

            public struct Enumerator : IEnumerator<V>
            {
                private readonly ValueSet _valueSet;
                private readonly int _count;
                private int _index;

                public Enumerator(ValueSet valueSet)
                {
                    _valueSet = valueSet;
                    _count = _valueSet.Count;
                    Debug.Assert(_count >= 1);
                    _index = -1;
                }

                public V Current
                {
                    get
                    {
                        return _valueSet[_index];
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public bool MoveNext()
                {
                    _index++;
                    return _index < _count;
                }

                public void Reset()
                {
                    _index = -1;
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
