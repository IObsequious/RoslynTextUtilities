// -----------------------------------------------------------------------
// <copyright file="CryptographicHashProvider.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct LinePositionSpan : IEquatable<LinePositionSpan>
    {
        public LinePositionSpan(LinePosition start, LinePosition end)
        {
            if (end < start)
            {
                throw new ArgumentException(CodeAnalysisResources.EndMustNotBeLessThanStart, nameof(end));
            }

            Start = start;
            End = end;
        }

        public LinePosition Start { get; }

        public LinePosition End { get; }

        public override bool Equals(object obj)
        {
            return obj is LinePositionSpan linePositionSpan
                && Equals(linePositionSpan);
        }

        public bool Equals(LinePositionSpan other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Start.GetHashCode(), End.GetHashCode());
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
            return string.Format(format: "({0})-({1})", arg0: Start, arg1: End);
        }
    }
}
