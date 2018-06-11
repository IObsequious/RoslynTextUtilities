using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Collections
{
    public class CachingDictionary<TKey, TElement>
    {
        private readonly Func<TKey, ImmutableArray<TElement>> _getElementsOfKey;
        private readonly Func<IEqualityComparer<TKey>, HashSet<TKey>> _getKeys;
        private readonly IEqualityComparer<TKey> _comparer;
        private IDictionary<TKey, ImmutableArray<TElement>> _map;
        private static readonly ImmutableArray<TElement> s_emptySentinel = ImmutableArray<TElement>.Empty;

        public CachingDictionary(
            Func<TKey, ImmutableArray<TElement>> getElementsOfKey,
            Func<IEqualityComparer<TKey>, HashSet<TKey>> getKeys,
            IEqualityComparer<TKey> comparer)
        {
            _getElementsOfKey = getElementsOfKey;
            _getKeys = getKeys;
            _comparer = comparer;
        }

        public bool Contains(TKey key)
        {
            return this[key].Length != 0;
        }

        public ImmutableArray<TElement> this[TKey key] => GetOrCreateValue(key);

        public int Count => EnsureFullyPopulated().Count;

        public IEnumerable<TKey> Keys => EnsureFullyPopulated().Keys;

        public void AddValues(ArrayBuilder<TElement> array)
        {
            foreach (KeyValuePair<TKey, ImmutableArray<TElement>> kvp in EnsureFullyPopulated())
            {
                array.AddRange(kvp.Value);
            }
        }

        private ConcurrentDictionary<TKey, ImmutableArray<TElement>> CreateConcurrentDictionary()
        {
            return new ConcurrentDictionary<TKey, ImmutableArray<TElement>>(2, 0, _comparer);
        }

        private IDictionary<TKey, ImmutableArray<TElement>> CreateDictionaryForFullyPopulatedMap(int capacity)
        {
            return new Dictionary<TKey, ImmutableArray<TElement>>(capacity, _comparer);
        }

        private ImmutableArray<TElement> GetOrCreateValue(TKey key)
        {
            ImmutableArray<TElement> elements;
            ConcurrentDictionary<TKey, ImmutableArray<TElement>> concurrentMap;
            IDictionary<TKey, ImmutableArray<TElement>> localMap = _map;
            if (localMap == null)
            {
                concurrentMap = CreateConcurrentDictionary();
                localMap = Interlocked.CompareExchange(ref _map, concurrentMap, null);
                if (localMap == null)
                {
                    return AddToConcurrentMap(concurrentMap, key);
                }
            }

            if (localMap.TryGetValue(key, out elements))
            {
                return elements;
            }

            concurrentMap = localMap as ConcurrentDictionary<TKey, ImmutableArray<TElement>>;
            return concurrentMap == null ? s_emptySentinel : AddToConcurrentMap(concurrentMap, key);
        }

        private ImmutableArray<TElement> AddToConcurrentMap(ConcurrentDictionary<TKey, ImmutableArray<TElement>> map, TKey key)
        {
            ImmutableArray<TElement> elements = _getElementsOfKey(key);
            if (elements.IsDefaultOrEmpty)
            {
                elements = s_emptySentinel;
            }

            return map.GetOrAdd(key, elements);
        }

        private static bool IsNotFullyPopulatedMap(IDictionary<TKey, ImmutableArray<TElement>> existingMap)
        {
            return existingMap == null || existingMap is ConcurrentDictionary<TKey, ImmutableArray<TElement>>;
        }

        private IDictionary<TKey, ImmutableArray<TElement>> CreateFullyPopulatedMap(IDictionary<TKey, ImmutableArray<TElement>> existingMap)
        {
            Debug.Assert(IsNotFullyPopulatedMap(existingMap));
            HashSet<TKey> allKeys = _getKeys(_comparer);
            Debug.Assert(_comparer == allKeys.Comparer);
            IDictionary<TKey, ImmutableArray<TElement>> fullyPopulatedMap = CreateDictionaryForFullyPopulatedMap(allKeys.Count);
            if (existingMap == null)
            {
                foreach (TKey key in allKeys)
                {
                    fullyPopulatedMap.Add(key, _getElementsOfKey(key));
                }
            }
            else
            {
                foreach (TKey key in allKeys)
                {
                    ImmutableArray<TElement> elements;
                    if (!existingMap.TryGetValue(key, out elements))
                    {
                        elements = _getElementsOfKey(key);
                    }

                    Debug.Assert(elements != s_emptySentinel);
                    fullyPopulatedMap.Add(key, elements);
                }
            }

            return fullyPopulatedMap;
        }

        private IDictionary<TKey, ImmutableArray<TElement>> EnsureFullyPopulated()
        {
            IDictionary<TKey, ImmutableArray<TElement>> fullyPopulatedMap = null;
            IDictionary<TKey, ImmutableArray<TElement>> currentMap = _map;
            while (IsNotFullyPopulatedMap(currentMap))
            {
                if (fullyPopulatedMap == null)
                {
                    fullyPopulatedMap = CreateFullyPopulatedMap(currentMap);
                }

                IDictionary<TKey, ImmutableArray<TElement>> replacedMap =
                    Interlocked.CompareExchange(ref _map, fullyPopulatedMap, currentMap);
                if (replacedMap == currentMap)
                {
                    return fullyPopulatedMap;
                }

                currentMap = replacedMap;
            }

            return currentMap;
        }
    }
}
