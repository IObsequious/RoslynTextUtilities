using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public sealed class OrderedMultiDictionary<K, V> : IEnumerable<KeyValuePair<K, SetWithInsertionOrder<V>>>
    {
        private readonly Dictionary<K, SetWithInsertionOrder<V>> _dictionary;
        private readonly List<K> _keys;

        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public IEnumerable<K> Keys
        {
            get
            {
                return _keys;
            }
        }

        public SetWithInsertionOrder<V> this[K k]
        {
            get
            {
                SetWithInsertionOrder<V> set;
                return _dictionary.TryGetValue(k, out set) ? set : new SetWithInsertionOrder<V>();
            }
        }

        public OrderedMultiDictionary()
        {
            _dictionary = new Dictionary<K, SetWithInsertionOrder<V>>();
            _keys = new List<K>();
        }

        public void Add(K k, V v)
        {
            SetWithInsertionOrder<V> set;
            if (!_dictionary.TryGetValue(k, out set))
            {
                _keys.Add(k);
                set = new SetWithInsertionOrder<V>();
            }

            set.Add(v);
            _dictionary[k] = set;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<K, SetWithInsertionOrder<V>>> GetEnumerator()
        {
            foreach (K key in _keys)
            {
                yield return new KeyValuePair<K, SetWithInsertionOrder<V>>(
                    key,
                    _dictionary[key]);
            }
        }
    }
}
