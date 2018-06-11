using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    public static class EnumerableExtensions
    {
        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>> items)
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>();
            }

            return ImmutableDictionary.CreateRange(items);
        }

        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>> items,
            IEqualityComparer<K> keyComparer)
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>(keyComparer);
            }

            return ImmutableDictionary.CreateRange(keyComparer, items);
        }

        public static IList<IList<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> data)
        {
#if DEBUG
            int count = data.First().Count();
            Debug.Assert(data.All(d => d.Count() == count));
#endif
            return TransposeInternal(data).ToArray();
        }

        private static IEnumerable<IList<T>> TransposeInternal<T>(this IEnumerable<IEnumerable<T>> data)
        {
            List<IEnumerator<T>> enumerators = new List<IEnumerator<T>>();
            int width = 0;
            foreach (IEnumerable<T> e in data)
            {
                enumerators.Add(e.GetEnumerator());
                width++;
            }

            try
            {
                while (true)
                {
                    T[] line = null;
                    for (int i = 0; i < width; i++)
                    {
                        IEnumerator<T> e = enumerators[i];
                        if (!e.MoveNext())
                        {
                            yield break;
                        }

                        if (line == null)
                        {
                            line = new T[width];
                        }

                        line[i] = e.Current;
                    }

                    yield return line;
                }
            }
            finally
            {
                foreach (IEnumerator<T> enumerator in enumerators)
                {
                    enumerator.Dispose();
                }
            }
        }

        public static void AddAllValues<K, T>(this IDictionary<K, ImmutableArray<T>> data, ArrayBuilder<T> builder)
        {
            foreach (ImmutableArray<T> values in data.Values)
            {
                builder.AddRange(values);
            }
        }

        public static Dictionary<K, ImmutableArray<T>> ToDictionary<K, T>(this IEnumerable<T> data,
            Func<T, K> keySelector,
            IEqualityComparer<K> comparer = null)
        {
            Dictionary<K, ImmutableArray<T>> dictionary = new Dictionary<K, ImmutableArray<T>>(comparer);
            IEnumerable<IGrouping<K, T>> groups = data.GroupBy(keySelector, comparer);
            foreach (IGrouping<K, T> grouping in groups)
            {
                ImmutableArray<T> items = grouping.AsImmutable();
                dictionary.Add(grouping.Key, items);
            }

            return dictionary;
        }

        public static TSource AsSingleton<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                return default;
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                return list.Count == 1 ? list[0] : default;
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return default;
                }

                TSource result = e.Current;
                if (e.MoveNext())
                {
                    return default;
                }

                return result;
            }
        }
    }
}
