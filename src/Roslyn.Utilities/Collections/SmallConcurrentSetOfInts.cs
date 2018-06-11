using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Collections
{
    public class SmallConcurrentSetOfInts
    {
        private int _v1;
        private int _v2;
        private int _v3;
        private int _v4;
        private SmallConcurrentSetOfInts _next;
        private const int unoccupied = int.MinValue;

        public SmallConcurrentSetOfInts()
        {
            _v1 = _v2 = _v3 = _v4 = unoccupied;
        }

        private SmallConcurrentSetOfInts(int initialValue)
        {
            _v1 = initialValue;
            _v2 = _v3 = _v4 = unoccupied;
        }

        public bool Contains(int i)
        {
            Debug.Assert(i != unoccupied);
            return Contains(this, i);
        }

        private static bool Contains(SmallConcurrentSetOfInts set, int i)
        {
            do
            {
                if (set._v1 == i || set._v2 == i || set._v3 == i || set._v4 == i)
                {
                    return true;
                }

                set = set._next;
            }
            while (set != null);

            return false;
        }

        public bool Add(int i)
        {
            Debug.Assert(i != unoccupied);
            return Add(this, i);
        }

        private static bool Add(SmallConcurrentSetOfInts set, int i)
        {
            bool added = false;
            while (true)
            {
                if (AddHelper(ref set._v1, i, ref added)
                    || AddHelper(ref set._v2, i, ref added)
                    || AddHelper(ref set._v3, i, ref added)
                    || AddHelper(ref set._v4, i, ref added))
                {
                    return added;
                }

                SmallConcurrentSetOfInts nextSet = set._next;
                if (nextSet == null)
                {
                    SmallConcurrentSetOfInts tail = new SmallConcurrentSetOfInts(i);
                    nextSet = Interlocked.CompareExchange(ref set._next, tail, null);
                    if (nextSet == null)
                    {
                        return true;
                    }
                }

                set = nextSet;
            }
        }

        private static bool AddHelper(ref int slot, int i, ref bool added)
        {
            Debug.Assert(!added);
            int val = slot;
            if (val == unoccupied)
            {
                val = Interlocked.CompareExchange(ref slot, i, unoccupied);
                if (val == unoccupied)
                {
                    added = true;
                    return true;
                }
            }

            return val == i;
        }
    }
}
