using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    public class ConcurrentCache<TKey, TValue> :
        CachingBase<ConcurrentCache<TKey, TValue>.Entry> where TKey : IEquatable<TKey>
    {
        public class Entry
        {
            internal readonly int hash;
            internal readonly TKey key;
            internal readonly TValue value;

            internal Entry(int hash, TKey key, TValue value)
            {
                this.hash = hash;
                this.key = key;
                this.value = value;
            }
        }

        public ConcurrentCache(int size)
            : base(size)
        {
        }

        public bool TryAdd(TKey key, TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & mask;
            Entry entry = entries[idx];
            if (entry != null && entry.hash == hash && entry.key.Equals(key))
            {
                return false;
            }

            entries[idx] = new Entry(hash, key, value);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & mask;
            Entry entry = entries[idx];
            if (entry != null && entry.hash == hash && entry.key.Equals(key))
            {
                value = entry.value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
