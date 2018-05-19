using System;
using System.Collections.Concurrent;

namespace Roslyn.Utilities
{
    public static class ConcurrentDictionaryExtensions
    {
        public static void Add<K, V>(this ConcurrentDictionary<K, V> dict, K key, V value)
        {
            if (!dict.TryAdd(key, value))
            {
                throw new ArgumentException(message: "adding a duplicate");
            }
        }
    }
}
