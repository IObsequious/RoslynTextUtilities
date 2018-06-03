using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    public class ConcurrentLruCache<K, V>
    {
        private readonly int _capacity;

        private struct CacheValue
        {
            public V Value;
            public LinkedListNode<K> Node;
        }

        private readonly Dictionary<K, CacheValue> _cache;
        private readonly LinkedList<K> _nodeList;
        private readonly object _lockObject = new object();

        public ConcurrentLruCache(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _cache = new Dictionary<K, CacheValue>(capacity);
            _nodeList = new LinkedList<K>();
        }

        public ConcurrentLruCache(KeyValuePair<K, V>[] array)
            : this(array.Length)
        {
            foreach (KeyValuePair<K, V> kvp in array)
            {
                UnsafeAdd(kvp.Key, kvp.Value, true);
            }
        }

        internal IEnumerable<KeyValuePair<K, V>> TestingEnumerable
        {
            get
            {
                lock (_lockObject)
                {
                    KeyValuePair<K, V>[] copy = new KeyValuePair<K, V>[_cache.Count];
                    int index = 0;
                    foreach (K key in _nodeList)
                    {
                        copy[index++] = new KeyValuePair<K, V>(key,
                            _cache[key].Value);
                    }

                    return copy;
                }
            }
        }

        public void Add(K key, V value)
        {
            lock (_lockObject)
            {
                UnsafeAdd(key, value, true);
            }
        }

        private void MoveNodeToTop(LinkedListNode<K> node)
        {
            if (!ReferenceEquals(_nodeList.First, node))
            {
                _nodeList.Remove(node);
                _nodeList.AddFirst(node);
            }
        }

        private void UnsafeEvictLastNode()
        {
            Debug.Assert(_capacity > 0);
            LinkedListNode<K> lastNode = _nodeList.Last;
            _nodeList.Remove(lastNode);
            _cache.Remove(lastNode.Value);
        }

        private void UnsafeAddNodeToTop(K key, V value)
        {
            LinkedListNode<K> node = new LinkedListNode<K>(key);
            _cache.Add(key, new CacheValue {Node = node, Value = value});
            _nodeList.AddFirst(node);
        }

        private void UnsafeAdd(K key, V value, bool throwExceptionIfKeyExists)
        {
            if (_cache.TryGetValue(key, out CacheValue result))
            {
                if (throwExceptionIfKeyExists)
                {
                    throw new ArgumentException(message: "Key already exists", paramName: nameof(key));
                }

                if (!result.Value.Equals(value))
                {
                    result.Value = value;
                    _cache[key] = result;
                    MoveNodeToTop(result.Node);
                }
            }
            else
            {
                if (_cache.Count == _capacity)
                {
                    UnsafeEvictLastNode();
                }

                UnsafeAddNodeToTop(key, value);
            }
        }

        public V this[K key]
        {
            get
            {
                if (TryGetValue(key, out V value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                lock (_lockObject)
                {
                    UnsafeAdd(key, value, false);
                }
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            lock (_lockObject)
            {
                return UnsafeTryGetValue(key, out value);
            }
        }

        public bool UnsafeTryGetValue(K key, out V value)
        {
            if (_cache.TryGetValue(key, out CacheValue result))
            {
                MoveNodeToTop(result.Node);
                value = result.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        public V GetOrAdd(K key, V value)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out V result))
                {
                    return result;
                }

                UnsafeAdd(key, value, true);
                return value;
            }
        }

        public V GetOrAdd(K key, Func<V> creator)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out V result))
                {
                    return result;
                }

                V value = creator();
                UnsafeAdd(key, value, true);
                return value;
            }
        }

        public V GetOrAdd<T>(K key, T arg, Func<T, V> creator)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out V result))
                {
                    return result;
                }

                V value = creator(arg);
                UnsafeAdd(key, value, true);
                return value;
            }
        }
    }
}
