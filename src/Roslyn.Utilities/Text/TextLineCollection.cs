using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Text
{
    public abstract class TextLineCollection : IReadOnlyList<TextLine>
    {
        public abstract int Count { get; }

        public abstract TextLine this[int index] { get; }

        public abstract int IndexOf(int position);

        public virtual TextLine GetLineFromPosition(int position)
        {
            return this[IndexOf(position)];
        }

        public virtual LinePosition GetLinePosition(int position)
        {
            TextLine line = GetLineFromPosition(position);
            return new LinePosition(line.LineNumber, position - line.Start);
        }

        public LinePositionSpan GetLinePositionSpan(TextSpan span)
        {
            return new LinePositionSpan(GetLinePosition(span.Start), GetLinePosition(span.End));
        }

        public int GetPosition(LinePosition position)
        {
            return this[position.Line].Start + position.Character;
        }

        public TextSpan GetTextSpan(LinePositionSpan span)
        {
            return TextSpan.FromBounds(GetPosition(span.Start), GetPosition(span.End));
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<TextLine> IEnumerable<TextLine>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [SuppressMessage(category: "Performance", checkId: "CA1067", Justification = "Equality not actually implemented")]
        public struct Enumerator : IEnumerator<TextLine>, IEnumerator
        {
            private readonly TextLineCollection _lines;
            private int _index;

            internal Enumerator(TextLineCollection lines, int index = -1)
            {
                _lines = lines;
                _index = index;
            }

            public TextLine Current
            {
                get
                {
                    int ndx = _index;
                    if (ndx >= 0 && ndx < _lines.Count)
                    {
                        return _lines[ndx];
                    }

                    return default(TextLine);
                }
            }

            public bool MoveNext()
            {
                if (_index < _lines.Count - 1)
                {
                    _index = _index + 1;
                    return true;
                }

                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            bool IEnumerator.MoveNext()
            {
                return MoveNext();
            }

            void IEnumerator.Reset()
            {
            }

            void IDisposable.Dispose()
            {
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }
    }
}
