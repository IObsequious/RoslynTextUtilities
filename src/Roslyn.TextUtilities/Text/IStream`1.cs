using System.Collections.Generic;

namespace System.Text
{
    public interface IStream<T>
    {
        int Length { get; }
        int Position { get; }
        T this[int position] { get; }
        T Peek();
        T Read();
        void Advance();
        void Write(T item);
    }
}
