using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    public class CachingFactory<TKey, TValue> : CachingBase<CachingFactory<TKey, TValue>.Entry>
    {
        public struct Entry
        {
            internal int hash;
            internal TValue value;
        }

        private readonly int _size;
        private readonly Func<TKey, TValue> _valueFactory;
        private readonly Func<TKey, int> _keyHash;
        private readonly Func<TKey, TValue, bool> _keyValueEquality;

        public CachingFactory(int size,
            Func<TKey, TValue> valueFactory,
            Func<TKey, int> keyHash,
            Func<TKey, TValue, bool> keyValueEquality) :
            base(size)
        {
            _size = size;
            _valueFactory = valueFactory;
            _keyHash = keyHash;
            _keyValueEquality = keyValueEquality;
        }

        public void Add(TKey key, TValue value)
        {
            int hash = GetKeyHash(key);
            int idx = hash & mask;
            entries[idx].hash = hash;
            entries[idx].value = value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = GetKeyHash(key);
            int idx = hash & mask;
            Entry[] entries = this.entries;
            if (entries[idx].hash == hash)
            {
                TValue candidate = entries[idx].value;
                if (_keyValueEquality(key, candidate))
                {
                    value = candidate;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public TValue GetOrMakeValue(TKey key)
        {
            int hash = GetKeyHash(key);
            int idx = hash & mask;
            Entry[] entries = this.entries;
            if (entries[idx].hash == hash)
            {
                TValue candidate = entries[idx].value;
                if (_keyValueEquality(key, candidate))
                {
                    return candidate;
                }
            }

            TValue value = _valueFactory(key);
            entries[idx].hash = hash;
            entries[idx].value = value;
            return value;
        }

        private int GetKeyHash(TKey key)
        {
            int result = _keyHash(key) | _size;
            Debug.Assert(result != 0);
            return result;
        }
    }

    public class CachingIdentityFactory<TKey, TValue> : CachingBase<CachingIdentityFactory<TKey, TValue>.Entry>
        where TKey : class
    {
        private readonly Func<TKey, TValue> _valueFactory;
        private readonly ObjectPool<CachingIdentityFactory<TKey, TValue>> _pool;

        public struct Entry
        {
            internal TKey key;
            internal TValue value;
        }

        public CachingIdentityFactory(int size, Func<TKey, TValue> valueFactory) :
            base(size)
        {
            _valueFactory = valueFactory;
        }

        public CachingIdentityFactory(int size, Func<TKey, TValue> valueFactory, ObjectPool<CachingIdentityFactory<TKey, TValue>> pool) :
            this(size, valueFactory)
        {
            _pool = pool;
        }

        public void Add(TKey key, TValue value)
        {
            int hash = RuntimeHelpers.GetHashCode(key);
            int idx = hash & mask;
            entries[idx].key = key;
            entries[idx].value = value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = RuntimeHelpers.GetHashCode(key);
            int idx = hash & mask;
            Entry[] entries = this.entries;
            if (entries[idx].key == key)
            {
                value = entries[idx].value;
                return true;
            }

            value = default;
            return false;
        }

        public TValue GetOrMakeValue(TKey key)
        {
            int hash = RuntimeHelpers.GetHashCode(key);
            int idx = hash & mask;
            Entry[] entries = this.entries;
            if (entries[idx].key == key)
            {
                return entries[idx].value;
            }

            TValue value = _valueFactory(key);
            entries[idx].key = key;
            entries[idx].value = value;
            return value;
        }

        public static ObjectPool<CachingIdentityFactory<TKey, TValue>> CreatePool(int size, Func<TKey, TValue> valueFactory)
        {
            ObjectPool<CachingIdentityFactory<TKey, TValue>> pool = null;
            return new ObjectPool<CachingIdentityFactory<TKey, TValue>>(
                () => new CachingIdentityFactory<TKey, TValue>(size, valueFactory, pool),
                Environment.ProcessorCount * 2);
        }

        public void Free()
        {
            ObjectPool<CachingIdentityFactory<TKey, TValue>> pool = _pool;
            pool?.Free(this);
        }
    }

    public abstract class CachingBase<TEntry>
    {
        protected readonly int mask;
        protected readonly TEntry[] entries;

        internal CachingBase(int size)
        {
            int alignedSize = AlignSize(size);
            mask = alignedSize - 1;
            entries = new TEntry[alignedSize];
        }

        private static int AlignSize(int size)
        {
            Debug.Assert(size > 0);
            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            return size + 1;
        }
    }
}
