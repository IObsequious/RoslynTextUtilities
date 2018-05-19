using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
    {
        public TextSpan(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start + length < start)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Start = start;
            Length = length;
        }

        public int Start { get; }

        public int End
        {
            get
            {
                return Start + Length;
            }
        }

        public int Length { get; }

        public bool IsEmpty
        {
            get
            {
                return Length == 0;
            }
        }

        public bool Contains(int position)
        {
            return unchecked((uint) (position - Start) < (uint) Length);
        }

        public bool Contains(TextSpan span)
        {
            return span.Start >= Start && span.End <= End;
        }

        public bool OverlapsWith(TextSpan span)
        {
            int overlapStart = Math.Max(Start, span.Start);
            int overlapEnd = Math.Min(End, span.End);
            return overlapStart < overlapEnd;
        }

        public TextSpan? Overlap(TextSpan span)
        {
            int overlapStart = Math.Max(Start, span.Start);
            int overlapEnd = Math.Min(End, span.End);
            return overlapStart < overlapEnd ? FromBounds(overlapStart, overlapEnd) : (TextSpan?) null;
        }

        public bool IntersectsWith(TextSpan span)
        {
            return span.Start <= End && span.End >= Start;
        }

        public bool IntersectsWith(int position)
        {
            return unchecked((uint) (position - Start) <= (uint) Length);
        }

        public TextSpan? Intersection(TextSpan span)
        {
            int intersectStart = Math.Max(Start, span.Start);
            int intersectEnd = Math.Min(End, span.End);
            return intersectStart <= intersectEnd ? FromBounds(intersectStart, intersectEnd) : (TextSpan?) null;
        }

        public static TextSpan FromBounds(int start, int end)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), CodeAnalysisResources.StartMustNotBeNegative);
            }

            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end), CodeAnalysisResources.EndMustNotBeLessThanStart);
            }

            return new TextSpan(start, end - start);
        }

        public static bool operator ==(TextSpan left, TextSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextSpan left, TextSpan right)
        {
            return !left.Equals(right);
        }

        public bool Equals(TextSpan other)
        {
            return Start == other.Start && Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            return obj is TextSpan && Equals((TextSpan) obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Start, Length);
        }

        public override string ToString()
        {
            return $"[{Start}..{End})";
        }

        public int CompareTo(TextSpan other)
        {
            int diff = Start - other.Start;
            if (diff != 0)
            {
                return diff;
            }

            return Length - other.Length;
        }
    }
}
