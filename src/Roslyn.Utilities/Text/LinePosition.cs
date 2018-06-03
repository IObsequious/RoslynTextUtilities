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
                return default(LinePosition);
            }
        }

        private readonly int _line;
        private readonly int _character;

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

            _line = line;
            _character = character;
        }

        public LinePosition(int character)
        {
            if (character < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }

            _line = -1;
            _character = character;
        }

        public int Line
        {
            get
            {
                return _line;
            }
        }

        public int Character
        {
            get
            {
                return _character;
            }
        }

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
            return obj is LinePosition && Equals((LinePosition) obj);
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
            int result = _line.CompareTo(other._line);
            return result != 0 ? result : _character.CompareTo(other.Character);
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
