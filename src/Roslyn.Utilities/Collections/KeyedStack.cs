using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Collections
{
    public class KeyedStack<T, R>
    {
        private readonly Dictionary<T, Stack<R>> _dict = new Dictionary<T, Stack<R>>();

        public void Push(T key, R value)
        {
            Stack<R> store;
            if (!_dict.TryGetValue(key, out store))
            {
                store = new Stack<R>();
                _dict.Add(key, store);
            }

            store.Push(value);
        }

        public bool TryPop(T key, out R value)
        {
            Stack<R> store;
            if (_dict.TryGetValue(key, out store) && store.Count > 0)
            {
                value = store.Pop();
                return true;
            }

            value = default;
            return false;
        }
    }
}
