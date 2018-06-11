using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    public sealed class WeakList<T> : IEnumerable<T>
        where T : class
    {
        public WeakList()
        {
            TestOnly_UnderlyingArray = Array.Empty<WeakReference<T>>();
        }

        private void Resize()
        {
            Debug.Assert(WeakCount == TestOnly_UnderlyingArray.Length);
            Debug.Assert(TestOnly_UnderlyingArray.Length == 0 || TestOnly_UnderlyingArray.Length >= MinimalNonEmptySize);
            int alive = TestOnly_UnderlyingArray.Length;
            int firstDead = -1;
            for (int i = 0; i < TestOnly_UnderlyingArray.Length; i++)
            {
                T target;
                if (!TestOnly_UnderlyingArray[i].TryGetTarget(out target))
                {
                    if (firstDead == -1)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive < TestOnly_UnderlyingArray.Length / 4)
            {
                Shrink(firstDead, alive);
            }
            else if (alive >= 3 * TestOnly_UnderlyingArray.Length / 4)
            {
                WeakReference<T>[] newItems = new WeakReference<T>[GetExpandedSize(TestOnly_UnderlyingArray.Length)];
                if (firstDead >= 0)
                {
                    Compact(firstDead, newItems);
                }
                else
                {
                    Array.Copy(TestOnly_UnderlyingArray, 0, newItems, 0, TestOnly_UnderlyingArray.Length);
                    Debug.Assert(WeakCount == TestOnly_UnderlyingArray.Length);
                }

                TestOnly_UnderlyingArray = newItems;
            }
            else
            {
                Compact(firstDead, TestOnly_UnderlyingArray);
            }

            Debug.Assert(TestOnly_UnderlyingArray.Length > 0 && WeakCount < 3 * TestOnly_UnderlyingArray.Length / 4, "length: " + TestOnly_UnderlyingArray.Length + " size: " + WeakCount);
        }

        private void Shrink(int firstDead, int alive)
        {
            int newSize = GetExpandedSize(alive);
            WeakReference<T>[] newItems = newSize == TestOnly_UnderlyingArray.Length ? TestOnly_UnderlyingArray : new WeakReference<T>[newSize];
            Compact(firstDead, newItems);
            TestOnly_UnderlyingArray = newItems;
        }

        private const int MinimalNonEmptySize = 4;

        private static int GetExpandedSize(int baseSize)
        {
            return Math.Max((baseSize * 2) + 1, MinimalNonEmptySize);
        }

        private void Compact(int firstDead, WeakReference<T>[] result)
        {
            Debug.Assert(TestOnly_UnderlyingArray[firstDead].IsNull());
            if (!ReferenceEquals(TestOnly_UnderlyingArray, result))
            {
                Array.Copy(TestOnly_UnderlyingArray, 0, result, 0, firstDead);
            }

            int oldSize = WeakCount;
            int j = firstDead;
            for (int i = firstDead + 1; i < oldSize; i++)
            {
                WeakReference<T> item = TestOnly_UnderlyingArray[i];
                T target;
                if (item.TryGetTarget(out target))
                {
                    result[j++] = item;
                }
            }

            WeakCount = j;
            if (ReferenceEquals(TestOnly_UnderlyingArray, result))
            {
                while (j < oldSize)
                {
                    TestOnly_UnderlyingArray[j++] = null;
                }
            }
        }

        public int WeakCount { get; private set; }

        public WeakReference<T> GetWeakReference(int index)
        {
            if (index < 0 || index >= WeakCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return TestOnly_UnderlyingArray[index];
        }

        public void Add(T item)
        {
            if (WeakCount == TestOnly_UnderlyingArray.Length)
            {
                Resize();
            }

            Debug.Assert(WeakCount < TestOnly_UnderlyingArray.Length);
            TestOnly_UnderlyingArray[WeakCount++] = new WeakReference<T>(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            int count = WeakCount;
            int alive = WeakCount;
            int firstDead = -1;
            for (int i = 0; i < count; i++)
            {
                T item;
                if (TestOnly_UnderlyingArray[i].TryGetTarget(out item))
                {
                    yield return item;
                }
                else
                {
                    if (firstDead < 0)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive == 0)
            {
                TestOnly_UnderlyingArray = Array.Empty<WeakReference<T>>();
                WeakCount = 0;
            }
            else if (alive < TestOnly_UnderlyingArray.Length / 4)
            {
                Shrink(firstDead, alive);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal WeakReference<T>[] TestOnly_UnderlyingArray { get; private set; }
    }
}
