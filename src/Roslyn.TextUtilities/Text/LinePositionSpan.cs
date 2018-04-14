// -----------------------------------------------------------------------
// <copyright file="LinePositionSpan.cs" company="Ollon, LLC">
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
    /// Immutable span represented by a pair of line number and index within the line.
    /// </summary>
    public struct LinePositionSpan : IEquatable<LinePositionSpan>
    {
        private readonly LinePosition _end;

        /// <summary>
        /// Creates <see cref="LinePositionSpan"/>.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <exception cref="ArgumentException"><paramref name="end"/> precedes <paramref name="start"/>.</exception>
        public LinePositionSpan(LinePosition start, LinePosition end)
        {
            if (end < start)
            {
                throw new ArgumentException(nameof(end));
            }

            Start = start;
            _end = end;
        }

        /// <summary>
        /// Gets the start position of the span.
        /// </summary>
        public LinePosition Start { get; }

        /// <summary>
        /// Gets the end position of the span.
        /// </summary>
        public LinePosition End
        {
            get
            {
                return _end;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is LinePositionSpan && Equals((LinePositionSpan)obj);
        }

        public bool Equals(LinePositionSpan other)
        {
            return Start.Equals(other.Start) && _end.Equals(other._end);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Start.GetHashCode(), _end.GetHashCode());
        }

        public static bool operator ==(LinePositionSpan left, LinePositionSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LinePositionSpan left, LinePositionSpan right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Provides a string representation for <see cref="LinePositionSpan"/>.
        /// </summary>
        /// <example>(0,0)-(5,6)</example>
        public override string ToString()
        {
            return string.Format("({0})-({1})", Start, _end);
        }
    }
}
