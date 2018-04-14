﻿// -----------------------------------------------------------------------
// <copyright file="LinePosition.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;

namespace System.Text
{
    /// <summary>
    /// Immutable representation of a line number and position within a SourceText instance.
    /// </summary>
    public struct LinePosition : IEquatable<LinePosition>, IComparable<LinePosition>
    {
        /// <summary>
        /// A <see cref="LinePosition"/> that represents position 0 at line 0.
        /// </summary>
        public static LinePosition Zero => default;

        /// <summary>
        /// Initializes a new instance of a <see cref="LinePosition"/> with the given line and character.
        /// </summary>
        /// <param name="line">
        /// The line of the line position. The first line in a file is defined as line 0 (zero based line numbering).
        /// </param>
        /// <param name="character">
        /// The character position in the line.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> or <paramref name="character"/> is less than zero. </exception>
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

        // internal constructor that supports a line number == -1.
        // VB allows users to specify a 1-based line number of 0 when processing
        // externalsource directives, which get decremented during conversion to 0-based line numbers.
        // in this case the line number can be -1.
        internal LinePosition(int character)
        {
            if (character < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }

            Line = -1;
            Character = character;
        }

        /// <summary>
        /// The line number. The first line in a file is defined as line 0 (zero based line numbering).
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The character position within the line.
        /// </summary>
        public int Character { get; }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public static bool operator ==(LinePosition left, LinePosition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are different.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public static bool operator !=(LinePosition left, LinePosition right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        /// <param name="other">The object to compare.</param>
        public bool Equals(LinePosition other)
        {
            return other.Line == Line && other.Character == Character;
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            return obj is LinePosition && Equals((LinePosition)obj);
        }

        /// <summary>
        /// Provides a hash function for <see cref="LinePosition"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(Line, Character);
        }

        /// <summary>
        /// Provides a string representation for <see cref="LinePosition"/>.
        /// </summary>
        /// <example>0,10</example>
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