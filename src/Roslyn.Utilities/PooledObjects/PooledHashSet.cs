using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public class PooledHashSet<T> : HashSet<T>
    {
        private readonly ObjectPool<PooledHashSet<T>> _pool;

        private PooledHashSet(ObjectPool<PooledHashSet<T>> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            Clear();
            _pool?.Free(this);
        }

        private static readonly ObjectPool<PooledHashSet<T>> s_poolInstance = CreatePool();

        public static ObjectPool<PooledHashSet<T>> CreatePool()
        {
            ObjectPool<PooledHashSet<T>> pool = null;
            pool = new ObjectPool<PooledHashSet<T>>(factory: () => new PooledHashSet<T>(pool), size: 128);
            return pool;
        }

        public static PooledHashSet<T> GetInstance()
        {
            PooledHashSet<T> instance = s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }
    }
}
