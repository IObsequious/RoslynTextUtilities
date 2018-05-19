using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    public class StringTable
    {
        private struct Entry
        {
            public int HashCode;
            public string Text;
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
        private readonly Entry[] _localTable = new Entry[LocalSize];
        private static readonly Entry[] s_sharedTable = new Entry[SharedSize];
        private int _localRandom = Environment.TickCount;
        private static int s_sharedRandom = Environment.TickCount;

        internal StringTable() :
            this(null)
        {
        }

        #region "Poolable"

        private StringTable(ObjectPool<StringTable> pool)
        {
            _pool = pool;
        }

        private readonly ObjectPool<StringTable> _pool;
        private static readonly ObjectPool<StringTable> s_staticPool = CreatePool();

        private static ObjectPool<StringTable> CreatePool()
        {
            ObjectPool<StringTable> pool = null;
            pool = new ObjectPool<StringTable>(factory: () => new StringTable(pool), size: Environment.ProcessorCount * 2);
            return pool;
        }

        public static StringTable GetInstance()
        {
            return s_staticPool.Allocate();
        }

        public void Free()
        {
            _pool.Free(this);
        }

        #endregion

        public string Add(char[] chars, int start, int len)
        {
            int hashCode = Hash.GetFNVHashCode(chars, start, len);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null && arr[idx].HashCode == hashCode)
            {
                string result = arr[idx].Text;
                if (TextEquals(result, chars, start, len))
                {
                    return result;
                }
            }

            string shared = FindSharedEntry(chars, start, len, hashCode);
            if (shared != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;
                return shared;
            }

            return AddItem(chars, start, len, hashCode);
        }

        public string Add(string chars, int start, int len)
        {
            int hashCode = Hash.GetFNVHashCode(chars, start, len);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null && arr[idx].HashCode == hashCode)
            {
                string result = arr[idx].Text;
                if (TextEquals(result, chars, start, len))
                {
                    return result;
                }
            }

            string shared = FindSharedEntry(chars, start, len, hashCode);
            if (shared != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;
                return shared;
            }

            return AddItem(chars, start, len, hashCode);
        }

        public string Add(char chars)
        {
            int hashCode = Hash.GetFNVHashCode(chars);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null)
            {
                string result = arr[idx].Text;
                if (text.Length == 1 && text[0] == chars)
                {
                    return result;
                }
            }

            string shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;
                return shared;
            }

            return AddItem(chars, hashCode);
        }

        public string Add(StringBuilder chars)
        {
            int hashCode = Hash.GetFNVHashCode(chars);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null && arr[idx].HashCode == hashCode)
            {
                string result = arr[idx].Text;
                if (TextEquals(result, chars))
                {
                    return result;
                }
            }

            string shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;
                return shared;
            }

            return AddItem(chars, hashCode);
        }

        public string Add(string chars)
        {
            int hashCode = Hash.GetFNVHashCode(chars);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            string text = arr[idx].Text;
            if (text != null && arr[idx].HashCode == hashCode)
            {
                string result = arr[idx].Text;
                if (result == chars)
                {
                    return result;
                }
            }

            string shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;
                return shared;
            }

            AddCore(chars, hashCode);
            return chars;
        }

        private static string FindSharedEntry(char[] chars, int start, int len, int hashCode)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars, start, len))
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

        private static string FindSharedEntry(string chars, int start, int len, int hashCode)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars, start, len))
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

        private static unsafe string FindSharedEntryASCII(int hashCode, byte* asciiChars, int length)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && TextEqualsASCII(e, asciiChars, length))
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

        private static string FindSharedEntry(char chars, int hashCode)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                if (e != null)
                {
                    if (e.Length == 1 && e[0] == chars)
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

        private static string FindSharedEntry(StringBuilder chars, int hashCode)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars))
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

        private static string FindSharedEntry(string chars, int hashCode)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            string e = null;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;
                if (e != null)
                {
                    if (hash == hashCode && e == chars)
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

        private string AddItem(char[] chars, int start, int len, int hashCode)
        {
            string text = new string(chars, start, len);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(string chars, int start, int len, int hashCode)
        {
            string text = chars.Substring(start, len);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(char chars, int hashCode)
        {
            string text = new string(chars, 1);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(StringBuilder chars, int hashCode)
        {
            string text = chars.ToString();
            AddCore(text, hashCode);
            return text;
        }

        private void AddCore(string chars, int hashCode)
        {
            AddSharedEntry(hashCode, chars);
            Entry[] arr = _localTable;
            int idx = LocalIdxFromHash(hashCode);
            arr[idx].HashCode = hashCode;
            arr[idx].Text = chars;
        }

        private void AddSharedEntry(int hashCode, string text)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            int i1 = LocalNextRandom() & SharedBucketSizeMask;
            idx = (idx + (i1 * i1 + i1) / 2) & SharedSizeMask;
            foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);
        }

        public static string AddShared(StringBuilder chars)
        {
            int hashCode = Hash.GetFNVHashCode(chars);
            string shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                return shared;
            }

            return AddSharedSlow(hashCode, chars);
        }

        private static string AddSharedSlow(int hashCode, StringBuilder builder)
        {
            string text = builder.ToString();
            AddSharedSlow(hashCode, text);
            return text;
        }

        public static unsafe string AddSharedUTF8(byte* bytes, int byteCount)
        {
            bool isAscii;
            int hashCode = Hash.GetFNVHashCode(bytes, byteCount, out isAscii);
            if (isAscii)
            {
                string shared = FindSharedEntryASCII(hashCode, bytes, byteCount);
                if (shared != null)
                {
                    return shared;
                }
            }

            return AddSharedSlow(hashCode, bytes, byteCount, isAscii);
        }

        private static unsafe string AddSharedSlow(int hashCode, byte* utf8Bytes, int byteCount, bool isAscii)
        {
            string text = System.Reflection.Metadata.MetadataStringDecoder.DefaultUTF8.GetString(utf8Bytes, byteCount);
            if (isAscii)
            {
                AddSharedSlow(hashCode, text);
            }

            return text;
        }

        private static void AddSharedSlow(int hashCode, string text)
        {
            Entry[] arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            int i1 = SharedNextRandom() & SharedBucketSizeMask;
            idx = (idx + (i1 * i1 + i1) / 2) & SharedSizeMask;
            foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);
        }

        private static int LocalIdxFromHash(int hash)
        {
            return hash & LocalSizeMask;
        }

        private static int SharedIdxFromHash(int hash)
        {
            return (hash ^ (hash >> LocalSizeBits)) & SharedSizeMask;
        }

        private int LocalNextRandom()
        {
            return _localRandom++;
        }

        private static int SharedNextRandom()
        {
            return Interlocked.Increment(ref s_sharedRandom);
        }

        public static bool TextEquals(string array, string text, int start, int length)
        {
            if (array.Length != length)
            {
                return false;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != text[start + i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TextEquals(string array, StringBuilder text)
        {
            if (array.Length != text.Length)
            {
                return false;
            }

            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (array[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static unsafe bool TextEqualsASCII(string text, byte* ascii, int length)
        {
#if DEBUG
            for (int i = 0; i < length; i++)
            {
                Debug.Assert((ascii[i] & 0x80) == 0, message: "The byte* input to this method must be valid ASCII.");
            }
#endif
            if (length != text.Length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (ascii[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TextEquals(string array, char[] text, int start, int length)
        {
            return array.Length == length && TextEqualsCore(array, text, start);
        }

        private static bool TextEqualsCore(string array, char[] text, int start)
        {
            int s = start;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != text[s])
                {
                    return false;
                }

                s++;
            }

            return true;
        }
    }
}
