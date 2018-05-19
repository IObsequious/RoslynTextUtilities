using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    public class TextKeyedCache<T> where T : class
    {
        private struct LocalEntry
        {
            public string Text;
            public int HashCode;
            public T Item;
        }

        private struct SharedEntry
        {
            public int HashCode;
            public SharedEntryValue Entry;
        }

        private class SharedEntryValue
        {
            public readonly string Text;
            public readonly T Item;

            public SharedEntryValue(string Text, T item)
            {
                this.Text = Text;
                Item = item;
            }
        }

        private const int LocalSizeBits = 11;
        private const int LocalSize = 1 << LocalSizeBits;
        private const int LocalSizeMask = LocalSize - 1;
        private const int SharedSizeBits = 16;
        private const int SharedSize = 1 << SharedSizeBits;
        private const int SharedSizeMask = SharedSize - 1;
        private const int SharedBucketBits = 4;
        private const int SharedBucketSize = 1 << SharedBucketBits;
        private const int SharedBucketSizeMask = SharedBucketSize - 1;
        private readonly LocalEntry[] _localTable = new LocalEntry[LocalSize];
        private static readonly SharedEntry[] s_sharedTable = new SharedEntry[SharedSize];
        private readonly SharedEntry[] _sharedTableInst = s_sharedTable;
        private readonly StringTable _strings;
        private Random _random;

        public TextKeyedCache() :
            this(null)
        {
        }

        #region "Poolable"

        private TextKeyedCache(ObjectPool<TextKeyedCache<T>> pool)
        {
            _pool = pool;
            _strings = new StringTable();
        }

        private readonly ObjectPool<TextKeyedCache<T>> _pool;
        private static readonly ObjectPool<TextKeyedCache<T>> s_staticPool = CreatePool();

        private static ObjectPool<TextKeyedCache<T>> CreatePool()
        {
            ObjectPool<TextKeyedCache<T>> pool = null;
            pool = new ObjectPool<TextKeyedCache<T>>(factory: () => new TextKeyedCache<T>(pool), size: Environment.ProcessorCount * 4);
            return pool;
        }

        public static TextKeyedCache<T> GetInstance()
        {
            return s_staticPool.Allocate();
        }

        public void Free()
        {
            _pool.Free(this);
        }

        #endregion

        public T FindItem(char[] chars, int start, int len, int hashCode)
        {
            LocalEntry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null && arr[idx].HashCode == hashCode)
            {
                if (StringTable.TextEquals(text, chars, start, len))
                {
                    return arr[idx].Item;
                }
            }

            SharedEntryValue e = FindSharedEntry(chars, start, len, hashCode);
            if (e != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = e.Text;
                T tk = e.Item;
                arr[idx].Item = tk;
                return tk;
            }

            return null;
        }

        private SharedEntryValue FindSharedEntry(char[] chars, int start, int len, int hashCode)
        {
            SharedEntry[] arr = _sharedTableInst;
            int idx = SharedIdxFromHash(hashCode);
            SharedEntryValue e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Entry;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && StringTable.TextEquals(e.Text, chars, start, len))
                    {
                        break;
                    }

                    e = null;
                }
                else
                {
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        public void AddItem(char[] chars, int start, int len, int hashCode, T item)
        {
            string text = _strings.Add(chars, start, len);
            SharedEntryValue e = new SharedEntryValue(text, item);
            AddSharedEntry(hashCode, e);
            LocalEntry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            arr[idx].HashCode = hashCode;
            arr[idx].Text = text;
            arr[idx].Item = item;
        }

        private void AddSharedEntry(int hashCode, SharedEntryValue e)
        {
            SharedEntry[] arr = _sharedTableInst;
            int idx = SharedIdxFromHash(hashCode);
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Entry == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            int i1 = NextRandom() & SharedBucketSizeMask;
            idx = (idx + (i1 * i1 + i1) / 2) & SharedSizeMask;
            foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Entry, e);
        }

        private static int LocalIdxFromHash(int hash)
        {
            return hash & LocalSizeMask;
        }

        private static int SharedIdxFromHash(int hash)
        {
            return (hash ^ (hash >> LocalSizeBits)) & SharedSizeMask;
        }

        private int NextRandom()
        {
            Random r = _random;
            if (r != null)
            {
                return r.Next();
            }

            r = new Random();
            _random = r;
            return r.Next();
        }
    }
}
