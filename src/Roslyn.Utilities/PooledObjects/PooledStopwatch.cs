using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public sealed class PooledStopwatch : Stopwatch
    {
        private static readonly ObjectPool<PooledStopwatch> s_poolInstance = CreatePool();
        private readonly ObjectPool<PooledStopwatch> _pool;

        private PooledStopwatch(ObjectPool<PooledStopwatch> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            Reset();
            _pool?.Free(this);
        }

        public static ObjectPool<PooledStopwatch> CreatePool()
        {
            ObjectPool<PooledStopwatch> pool = null;
            return new ObjectPool<PooledStopwatch>(factory: () => new PooledStopwatch(pool), size: 128);
        }

        public static PooledStopwatch StartInstance()
        {
            PooledStopwatch instance = s_poolInstance.Allocate();
            instance.Restart();
            return instance;
        }
    }
}
