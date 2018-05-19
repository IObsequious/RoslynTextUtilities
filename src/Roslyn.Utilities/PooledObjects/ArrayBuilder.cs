using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    [DebuggerDisplay(value: "Count = {Count,nq}")]
    [DebuggerTypeProxy(typeof(ArrayBuilder<>.DebuggerProxy))]
    public sealed partial class ArrayBuilder<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        #region DebuggerProxy

        private sealed class DebuggerProxy
        {
            private readonly ArrayBuilder<T> _builder;

            public DebuggerProxy(ArrayBuilder<T> builder)
            {
                _builder = builder;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] A
            {
                get
                {
                    T[] result = new T[_builder.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = _builder[i];
                    }

                    return result;
                }
            }
        }

        #endregion

        private readonly ImmutableArray<T>.Builder _builder;
        private readonly ObjectPool<ArrayBuilder<T>> _pool;

        public ArrayBuilder(int size)
        {
            _builder = ImmutableArray.CreateBuilder<T>(size);
        }

        public ArrayBuilder() :
            this(8)
        {
        }

        private ArrayBuilder(ObjectPool<ArrayBuilder<T>> pool) :
            this()
        {
            _pool = pool;
        }

        public ImmutableArray<T> ToImmutable()
        {
            return _builder.ToImmutable();
        }

        public int Count
        {
            get
            {
                return _builder.Count;
            }
            set
            {
                _builder.Count = value;
            }
        }

        public T this[int index]
        {
            get
            {
                return _builder[index];
            }
            set
            {
                _builder[index] = value;
            }
        }

        public void SetItem(int index, T value)
        {
            while (index > _builder.Count)
            {
                _builder.Add(default);
            }

            if (index == _builder.Count)
            {
                _builder.Add(value);
            }
            else
            {
                _builder[index] = value;
            }
        }

        public void Add(T item)
        {
            _builder.Add(item);
        }

        public void Insert(int index, T item)
        {
            _builder.Insert(index, item);
        }

        public void EnsureCapacity(int capacity)
        {
            if (_builder.Capacity < capacity)
            {
                _builder.Capacity = capacity;
            }
        }

        public void Clear()
        {
            _builder.Clear();
        }

        public bool Contains(T item)
        {
            return _builder.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _builder.IndexOf(item);
        }

        public int IndexOf(T item, IEqualityComparer<T> equalityComparer)
        {
            return _builder.IndexOf(item, 0, _builder.Count, equalityComparer);
        }

        public int IndexOf(T item, int startIndex, int count)
        {
            return _builder.IndexOf(item, startIndex, count);
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, Count, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, Count - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(_builder[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveAt(int index)
        {
            _builder.RemoveAt(index);
        }

        public void RemoveLast()
        {
            _builder.RemoveAt(_builder.Count - 1);
        }

        public void ReverseContents()
        {
            _builder.Reverse();
        }

        public void Sort()
        {
            _builder.Sort();
        }

        public void Sort(IComparer<T> comparer)
        {
            _builder.Sort(comparer);
        }

        public void Sort(Comparison<T> compare)
        {
            Sort(Comparer<T>.Create(compare));
        }

        public void Sort(int startIndex, IComparer<T> comparer)
        {
            _builder.Sort(startIndex, _builder.Count - startIndex, comparer);
        }

        public T[] ToArray()
        {
            return _builder.ToArray();
        }

        public void CopyTo(T[] array, int start)
        {
            _builder.CopyTo(array, start);
        }

        public T Last()
        {
            return _builder[_builder.Count - 1];
        }

        public T First()
        {
            return _builder[0];
        }

        public bool Any()
        {
            return _builder.Count > 0;
        }

        public ImmutableArray<T> ToImmutableOrNull()
        {
            if (Count == 0)
            {
                return default;
            }

            return ToImmutable();
        }

        public ImmutableArray<U> ToDowncastedImmutable<U>()
            where U : T
        {
            if (Count == 0)
            {
                return ImmutableArray<U>.Empty;
            }

            ArrayBuilder<U> tmp = ArrayBuilder<U>.GetInstance(Count);
            foreach (T i in this)
            {
                tmp.Add((U) i);
            }

            return tmp.ToImmutableAndFree();
        }

        public ImmutableArray<T> ToImmutableAndFree()
        {
            ImmutableArray<T> result;
            if (_builder.Capacity == Count)
            {
                result = _builder.MoveToImmutable();
            }
            else
            {
                result = ToImmutable();
            }

            Free();
            return result;
        }

        public T[] ToArrayAndFree()
        {
            T[] result = ToArray();
            Free();
            return result;
        }

        #region Poolable

        public void Free()
        {
            ObjectPool<ArrayBuilder<T>> pool = _pool;
            if (pool != null)
            {
                if (_builder.Capacity < 128)
                {
                    if (Count != 0)
                    {
                        Clear();
                    }

                    pool.Free(this);
                }
                else
                {
                    pool.ForgetTrackedObject(this);
                }
            }
        }

        private static readonly ObjectPool<ArrayBuilder<T>> s_poolInstance = CreatePool();

        public static ArrayBuilder<T> GetInstance()
        {
            ArrayBuilder<T> builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Count == 0);
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity)
        {
            ArrayBuilder<T> builder = GetInstance();
            builder.EnsureCapacity(capacity);
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity, T fillWithValue)
        {
            ArrayBuilder<T> builder = GetInstance();
            builder.EnsureCapacity(capacity);
            for (int i = 0; i < capacity; i++)
            {
                builder.Add(fillWithValue);
            }

            return builder;
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool()
        {
            return CreatePool(128);
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool(int size)
        {
            ObjectPool<ArrayBuilder<T>> pool = null;
            pool = new ObjectPool<ArrayBuilder<T>>(factory: () => new ArrayBuilder<T>(pool), size: size);
            return pool;
        }

        #endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<K, ImmutableArray<T>> ToDictionary<K>(Func<T, K> keySelector, IEqualityComparer<K> comparer = null)
        {
            if (Count == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = this[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (Count == 0)
            {
                return new Dictionary<K, ImmutableArray<T>>(comparer);
            }

            Dictionary<K, ArrayBuilder<T>> accumulator = new Dictionary<K, ArrayBuilder<T>>(Count, comparer);
            for (int i = 0; i < Count; i++)
            {
                T item = this[i];
                K key = keySelector(item);
                if (!accumulator.TryGetValue(key, out ArrayBuilder<T> bucket))
                {
                    bucket = GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new Dictionary<K, ImmutableArray<T>>(accumulator.Count, comparer);
            foreach (KeyValuePair<K, ArrayBuilder<T>> pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }

        public void AddRange(ArrayBuilder<T> items)
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange<U>(ArrayBuilder<U> items) where U : T
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange(ImmutableArray<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(ImmutableArray<T> items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void AddRange<S>(ImmutableArray<S> items) where S : class, T
        {
            AddRange(ImmutableArray<T>.CastUp(items));
        }

        public void AddRange(T[] items, int start, int length)
        {
            for (int i = start, end = start + length; i < end; i++)
            {
                Add(items[i]);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(params T[] items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(T[] items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void Clip(int limit)
        {
            Debug.Assert(limit <= Count);
            _builder.Count = limit;
        }

        public void ZeroInit(int count)
        {
            _builder.Clear();
            _builder.Count = count;
        }

        public void AddMany(T item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Add(item);
            }
        }

        public void RemoveDuplicates()
        {
            PooledHashSet<T> set = PooledHashSet<T>.GetInstance();
            int j = 0;
            for (int i = 0; i < Count; i++)
            {
                if (set.Add(this[i]))
                {
                    this[j] = this[i];
                    j++;
                }
            }

            Clip(j);
            set.Free();
        }

        public ImmutableArray<S> SelectDistinct<S>(Func<T, S> selector)
        {
            ArrayBuilder<S> result = ArrayBuilder<S>.GetInstance(Count);
            PooledHashSet<S> set = PooledHashSet<S>.GetInstance();
            foreach (T item in this)
            {
                S selected = selector(item);
                if (set.Add(selected))
                {
                    result.Add(selected);
                }
            }

            set.Free();
            return result.ToImmutableAndFree();
        }
    }
}
