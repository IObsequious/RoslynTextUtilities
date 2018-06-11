using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct LinePosition : IEquatable<LinePosition>, IComparable<LinePosition>
    {
        public static LinePosition Zero
        {
            get
            {
                return default;
            }
        }

        public LinePosition(int line, int character)
        {
            if (line < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(line));
            }

            if (character < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }

            Line = line;
            Character = character;
        }

        public LinePosition(int character)
        {
            if (character < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }

            Line = -1;
            Character = character;
        }

        public int Line { get; }

        public int Character { get; }

        public static bool operator ==(LinePosition left, LinePosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LinePosition left, LinePosition right)
        {
            return !left.Equals(right);
        }

        public bool Equals(LinePosition other)
        {
            return other.Line == Line && other.Character == Character;
        }

        public override bool Equals(object obj)
        {
            return obj is LinePosition linePosition
                && Equals(linePosition);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Line, Character);
        }

        public override string ToString()
        {
            return Line + "," + Character;
        }

        public int CompareTo(LinePosition other)
        {
            int result = Line.CompareTo(other.Line);
            return result != 0 ? result : Character.CompareTo(other.Character);
        }

        public static bool operator >(LinePosition left, LinePosition right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(LinePosition left, LinePosition right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(LinePosition left, LinePosition right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(LinePosition left, LinePosition right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
