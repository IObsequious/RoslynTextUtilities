using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    public static class ImmutableArrayExtensions
    {
        public static ImmutableArray<T> AsImmutable<T>(this IEnumerable<T> items)
        {
            return ImmutableArray.CreateRange<T>(items);
        }

        public static ImmutableArray<T> AsImmutableOrEmpty<T>(this IEnumerable<T> items)
        {
            if (items == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        public static ImmutableArray<T> AsImmutableOrNull<T>(this IEnumerable<T> items)
        {
            if (items == null)
            {
                return default;
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        public static ImmutableArray<T> AsImmutable<T>(this T[] items)
        {
            Debug.Assert(items != null);
            return ImmutableArray.Create<T>(items);
        }

        public static ImmutableArray<T> AsImmutableOrNull<T>(this T[] items)
        {
            if (items == null)
            {
                return default;
            }

            return ImmutableArray.Create<T>(items);
        }

        public static ImmutableArray<T> AsImmutableOrEmpty<T>(this T[] items)
        {
            if (items == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return ImmutableArray.Create<T>(items);
        }

        public static ImmutableArray<byte> ToImmutable(this MemoryStream stream)
        {
            return ImmutableArray.Create<byte>(stream.ToArray());
        }

        public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ImmutableArray<TItem> items, Func<TItem, TResult> map)
        {
            return ImmutableArray.CreateRange(items, map);
        }

        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items,
            Func<TItem, TArg, TResult> map,
            TArg arg)
        {
            return ImmutableArray.CreateRange(items, map, arg);
        }

        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items,
            Func<TItem, int, TArg, TResult> map,
            TArg arg)
        {
            switch (items.Length)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;
                case 1:
                    return ImmutableArray.Create(map(items[0], 0, arg));
                case 2:
                    return ImmutableArray.Create(map(items[0], 0, arg), map(items[1], 1, arg));
                case 3:
                    return ImmutableArray.Create(map(items[0], 0, arg), map(items[1], 1, arg), map(items[2], 2, arg));
                case 4:
                    return ImmutableArray.Create(map(items[0], 0, arg),
                        map(items[1], 1, arg),
                        map(items[2], 2, arg),
                        map(items[3], 3, arg));
                default:
                    ArrayBuilder<TResult> builder = ArrayBuilder<TResult>.GetInstance(items.Length);
                    for (int i = 0; i < items.Length; i++)
                    {
                        builder.Add(map(items[i], i, arg));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static ImmutableArray<TResult> ZipAsArray<T1, T2, TResult>(this ImmutableArray<T1> self,
            ImmutableArray<T2> other,
            Func<T1, T2, TResult> map)
        {
            Debug.Assert(self.Length == other.Length);
            switch (self.Length)
            {
                case 0:
                    return ImmutableArray<TResult>.Empty;
                case 1:
                    return ImmutableArray.Create(map(self[0], other[0]));
                case 2:
                    return ImmutableArray.Create(map(self[0], other[0]), map(self[1], other[1]));
                case 3:
                    return ImmutableArray.Create(map(self[0], other[0]), map(self[1], other[1]), map(self[2], other[2]));
                case 4:
                    return ImmutableArray.Create(map(self[0], other[0]),
                        map(self[1], other[1]),
                        map(self[2], other[2]),
                        map(self[3], other[3]));
                default:
                    ArrayBuilder<TResult> builder = ArrayBuilder<TResult>.GetInstance(self.Length);
                    for (int i = 0; i < self.Length; i++)
                    {
                        builder.Add(map(self[i], other[i]));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, bool> predicate)
        {
            Debug.Assert(!array.IsDefault);
            ArrayBuilder<T> builder = null;
            bool none = true;
            bool all = true;
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];
                if (predicate(a))
                {
                    none = false;
                    if (all)
                    {
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (builder == null)
                    {
                        builder = ArrayBuilder<T>.GetInstance();
                    }

                    builder.Add(a);
                }
                else
                {
                    if (none)
                    {
                        all = false;
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (all)
                    {
                        Debug.Assert(builder == null);
                        all = false;
                        builder = ArrayBuilder<T>.GetInstance();
                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(array[j]);
                        }
                    }
                }
            }

            if (builder != null)
            {
                Debug.Assert(!all);
                Debug.Assert(!none);
                return builder.ToImmutableAndFree();
            }

            if (all)
            {
                return array;
            }

            Debug.Assert(none);
            return ImmutableArray<T>.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ImmutableArray<TBase> Cast<TDerived, TBase>(this ImmutableArray<TDerived> items)
            where TDerived : class, TBase
        {
            return ImmutableArray<TBase>.CastUp(items);
        }

        public static bool SetEquals<T>(this ImmutableArray<T> array1, ImmutableArray<T> array2, IEqualityComparer<T> comparer)
        {
            if (array1.IsDefault)
            {
                return array2.IsDefault;
            }

            if (array2.IsDefault)
            {
                return false;
            }

            var count1 = array1.Length;
            var count2 = array2.Length;
            if (count1 == 0)
            {
                return count2 == 0;
            }

            if (count2 == 0)
            {
                return false;
            }

            if (count1 == 1 && count2 == 1)
            {
                var item1 = array1[0];
                var item2 = array2[0];
                return comparer.Equals(item1, item2);
            }

            HashSet<T> set1 = new HashSet<T>(array1, comparer);
            HashSet<T> set2 = new HashSet<T>(array2, comparer);
            return set1.SetEquals(set2);
        }

        public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array)
        {
            return array.IsDefault ? ImmutableArray<T>.Empty : array;
        }

        public static ImmutableArray<T> Distinct<T>(this ImmutableArray<T> array, IEqualityComparer<T> comparer = null)
        {
            Debug.Assert(!array.IsDefault);
            if (array.Length < 2)
            {
                return array;
            }

            HashSet<T> set = new HashSet<T>(comparer);
            ArrayBuilder<T> builder = ArrayBuilder<T>.GetInstance();
            foreach (var a in array)
            {
                if (set.Add(a))
                {
                    builder.Add(a);
                }
            }

            var result = builder.Count == array.Length ? array : builder.ToImmutable();
            builder.Free();
            return result;
        }

        public static bool HasAnyErrors<T>(this ImmutableArray<T> diagnostics) where T : Diagnostic
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        public static ImmutableArray<T> DeOrder<T>(this ImmutableArray<T> array)
        {
            if (!array.IsDefault && array.Length >= 2)
            {
                T[] copy = array.ToArray();
                int last = copy.Length - 1;
                T temp = copy[0];
                copy[0] = copy[last];
                copy[last] = temp;
                return copy.AsImmutable();
            }

            return array;
        }

        public static ImmutableArray<TValue> Flatten<TKey, TValue>(
            this Dictionary<TKey, ImmutableArray<TValue>> dictionary,
            IComparer<TValue> comparer = null)
        {
            if (dictionary.Count == 0)
            {
                return ImmutableArray<TValue>.Empty;
            }

            ArrayBuilder<TValue> builder = ArrayBuilder<TValue>.GetInstance();
            foreach (var kvp in dictionary)
            {
                builder.AddRange(kvp.Value);
            }

            if (comparer != null && builder.Count > 1)
            {
                builder.Sort(comparer);
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
        {
            return first.AddRange(second);
        }

        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, T second)
        {
            return first.Add(second);
        }

        public static bool HasDuplicates<T>(this ImmutableArray<T> array, IEqualityComparer<T> comparer)
        {
            switch (array.Length)
            {
                case 0:
                case 1:
                    return false;
                case 2:
                    return comparer.Equals(array[0], array[1]);
                default:
                    HashSet<T> set = new HashSet<T>(comparer);
                    foreach (var i in array)
                    {
                        if (!set.Add(i))
                        {
                            return true;
                        }
                    }

                    return false;
            }
        }

        public static int Count<T>(this ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Length; ++i)
            {
                if (predicate(items[i]))
                {
                    ++count;
                }
            }

            return count;
        }

        public static Dictionary<K, ImmutableArray<T>> ToDictionary<K, T>(this ImmutableArray<T> items,
            Func<T, K> keySelector,
            IEqualityComparer<K> comparer = null)
        {
            if (items.Length == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = items[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (items.Length == 0)
            {
                return new Dictionary<K, ImmutableArray<T>>(comparer);
            }

            Dictionary<K, ArrayBuilder<T>> accumulator = new Dictionary<K, ArrayBuilder<T>>(items.Length, comparer);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                K key = keySelector(item);
                if (!accumulator.TryGetValue(key, out ArrayBuilder<T> bucket))
                {
                    bucket = ArrayBuilder<T>.GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new Dictionary<K, ImmutableArray<T>>(accumulator.Count, comparer);
            foreach (KeyValuePair<K, ArrayBuilder<T>> pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }
    }
}
