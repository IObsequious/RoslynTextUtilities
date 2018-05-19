using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public static class ISetExtensions
    {
        public static bool AddAll<T>(this ISet<T> set, IEnumerable<T> values)
        {
            bool result = false;
            foreach (T v in values)
            {
                result |= set.Add(v);
            }

            return result;
        }

        public static bool RemoveAll<T>(this ISet<T> set, IEnumerable<T> values)
        {
            bool result = false;
            foreach (T v in values)
            {
                result |= set.Remove(v);
            }

            return result;
        }
    }
}
