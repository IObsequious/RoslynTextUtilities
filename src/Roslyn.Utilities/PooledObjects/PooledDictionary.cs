using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public class PooledDictionary<K, V> : Dictionary<K, V>
    {
        private readonly ObjectPool<PooledDictionary<K, V>> _pool;

        private PooledDictionary(ObjectPool<PooledDictionary<K, V>> pool)
        {
            _pool = pool;
        }

        public ImmutableDictionary<K, V> ToImmutableDictionaryAndFree()
        {
            var result = this.ToImmutableDictionary();
            Free();
            return result;
        }

        public void Free()
        {
            Clear();
            _pool?.Free(this);
        }

        private static readonly ObjectPool<PooledDictionary<K, V>> s_poolInstance = CreatePool();

        public static ObjectPool<PooledDictionary<K, V>> CreatePool()
        {
            ObjectPool<PooledDictionary<K, V>> pool = null;
            pool = new ObjectPool<PooledDictionary<K, V>>(factory: () => new PooledDictionary<K, V>(pool), size: 128);
            return pool;
        }

        public static PooledDictionary<K, V> GetInstance()
        {
            PooledDictionary<K, V> instance = s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }
    }
}
