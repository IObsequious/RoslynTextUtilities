using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct LinePositionSpan : IEquatable<LinePositionSpan>
    {
        private readonly LinePosition _start;
        private readonly LinePosition _end;

        public LinePositionSpan(LinePosition start, LinePosition end)
        {
            if (end < start)
            {
                throw new ArgumentException(CodeAnalysisResources.EndMustNotBeLessThanStart, nameof(end));
            }

            _start = start;
            _end = end;
        }

        public LinePosition Start
        {
            get
            {
                return _start;
            }
        }

        public LinePosition End
        {
            get
            {
                return _end;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is LinePositionSpan && Equals((LinePositionSpan) obj);
        }

        public bool Equals(LinePositionSpan other)
        {
            return _start.Equals(other._start) && _end.Equals(other._end);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_start.GetHashCode(), _end.GetHashCode());
        }

        public static bool operator ==(LinePositionSpan left, LinePositionSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LinePositionSpan left, LinePositionSpan right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format(format: "({0})-({1})", arg0: _start, arg1: _end);
        }
    }
}
