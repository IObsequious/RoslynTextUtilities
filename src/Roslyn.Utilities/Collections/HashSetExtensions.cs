using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public static class HashSetExtensions
    {
        public static bool IsNullOrEmpty<T>(this HashSet<T> hashSet)
        {
            return hashSet == null || hashSet.Count == 0;
        }

        public static bool InitializeAndAdd<T>(ref HashSet<T> hashSet, T item) where T : class
        {
            if (item is null)
            {
                return false;
            }

            if (hashSet is null)
            {
                hashSet = new HashSet<T>();
            }

            return hashSet.Add(item);
        }
    }
}
