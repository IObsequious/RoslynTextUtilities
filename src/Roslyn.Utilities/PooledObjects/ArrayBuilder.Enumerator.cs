using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    public partial class ArrayBuilder<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayBuilder<T> _builder;
            private int _index;

            public Enumerator(ArrayBuilder<T> builder)
            {
                _builder = builder;
                _index = -1;
            }

            public T Current
            {
                get
                {
                    return _builder[_index];
                }
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _builder.Count;
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
