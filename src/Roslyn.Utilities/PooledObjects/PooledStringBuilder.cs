using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public class PooledStringBuilder
    {
        public readonly StringBuilder Builder = new StringBuilder();
        private readonly ObjectPool<PooledStringBuilder> _pool;

        private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
        {
            Debug.Assert(pool != null);
            _pool = pool;
        }

        public int Length
        {
            get
            {
                return Builder.Length;
            }
        }

        public void Free()
        {
            StringBuilder builder = Builder;
            if (builder.Capacity <= 1024)
            {
                builder.Clear();
                _pool.Free(this);
            }
            else
            {
                _pool.ForgetTrackedObject(this);
            }
        }

        [Obsolete(message: "Consider calling ToStringAndFree instead.")]
        public new string ToString()
        {
            return Builder.ToString();
        }

        public string ToStringAndFree()
        {
            string result = Builder.ToString();
            Free();
            return result;
        }

        public string ToStringAndFree(int startIndex, int length)
        {
            string result = Builder.ToString(startIndex, length);
            Free();
            return result;
        }

        private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

        public static ObjectPool<PooledStringBuilder> CreatePool(int size = 32)
        {
            ObjectPool<PooledStringBuilder> pool = null;
            pool = new ObjectPool<PooledStringBuilder>(factory: () => new PooledStringBuilder(pool), size: size);
            return pool;
        }

        public static PooledStringBuilder GetInstance()
        {
            PooledStringBuilder builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Builder.Length == 0);
            return builder;
        }

        public static implicit operator StringBuilder(PooledStringBuilder obj)
        {
            return obj.Builder;
        }
    }
}
