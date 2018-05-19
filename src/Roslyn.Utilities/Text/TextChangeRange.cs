using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct TextChangeRange : IEquatable<TextChangeRange>
    {
        public TextSpan Span { get; }

        public int NewLength { get; }

        public TextChangeRange(TextSpan span, int newLength)
            : this()
        {
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newLength));
            }

            Span = span;
            NewLength = newLength;
        }

        public bool Equals(TextChangeRange other)
        {
            return
                other.Span == Span &&
                other.NewLength == NewLength;
        }

        public override bool Equals(object obj)
        {
            return obj is TextChangeRange && Equals((TextChangeRange) obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(NewLength, Span.GetHashCode());
        }

        public static bool operator ==(TextChangeRange left, TextChangeRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextChangeRange left, TextChangeRange right)
        {
            return !(left == right);
        }

        public static IReadOnlyList<TextChangeRange> NoChanges
        {
            get
            {
                return SpecializedCollections.EmptyReadOnlyList<TextChangeRange>();
            }
        }

        public static TextChangeRange Collapse(IEnumerable<TextChangeRange> changes)
        {
            int diff = 0;
            int start = int.MaxValue;
            int end = 0;
            foreach (TextChangeRange change in changes)
            {
                diff += change.NewLength - change.Span.Length;
                if (change.Span.Start < start)
                {
                    start = change.Span.Start;
                }

                if (change.Span.End > end)
                {
                    end = change.Span.End;
                }
            }

            if (start > end)
            {
                return default(TextChangeRange);
            }

            TextSpan combined = TextSpan.FromBounds(start, end);
            int newLen = combined.Length + diff;
            return new TextChangeRange(combined, newLen);
        }
    }
}
