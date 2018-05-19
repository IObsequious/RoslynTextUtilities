using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    public static class ImmutableListExtensions
    {
        public static ImmutableList<T> ToImmutableListOrEmpty<T>(this T[] items)
        {
            if (items == null)
            {
                return ImmutableList.Create<T>();
            }

            return ImmutableList.Create<T>(items);
        }

        public static ImmutableList<T> ToImmutableListOrEmpty<T>(this IEnumerable<T> items)
        {
            if (items == null)
            {
                return ImmutableList.Create<T>();
            }

            return ImmutableList.CreateRange<T>(items);
        }
    }
}
