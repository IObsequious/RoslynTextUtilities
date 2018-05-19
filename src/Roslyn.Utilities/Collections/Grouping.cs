using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public TKey Key { get; }

        private readonly IEnumerable<TElement> _elements;

        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            Key = key;
            _elements = elements;
        }

        public Grouping(KeyValuePair<TKey, IEnumerable<TElement>> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
