using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    public static class ImmutableArrayExtensions
    {
        public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null ? ImmutableArray<T>.Empty : ImmutableArray.CreateRange(items);
        }

        public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this ImmutableArray<T> items)
        {
            return items.IsDefault ? ImmutableArray<T>.Empty : items;
        }

        public static int BinarySearch<TElement, TValue>(this ImmutableArray<TElement> array,
            TValue value,
            Func<TElement, TValue, int> comparer)
        {
            int low = 0;
            int high = array.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                int comparison = comparer(array[middle], value);
                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }
    }
}
