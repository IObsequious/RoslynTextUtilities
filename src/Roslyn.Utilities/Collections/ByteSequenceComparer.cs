using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    public sealed class ByteSequenceComparer : IEqualityComparer<byte[]>, IEqualityComparer<ImmutableArray<byte>>
    {
        internal static readonly ByteSequenceComparer Instance = new ByteSequenceComparer();

        private ByteSequenceComparer()
        {
        }

        public static bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            if (x == y)
            {
                return true;
            }

            if (x.IsDefault || y.IsDefault || x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Equals(byte[] left, int leftStart, byte[] right, int rightStart, int length)
        {
            if (left == null || right == null)
            {
                return ReferenceEquals(left, right);
            }

            if (ReferenceEquals(left, right) && leftStart == rightStart)
            {
                return true;
            }

            for (int i = 0; i < length; i++)
            {
                if (left[leftStart + i] != right[rightStart + i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Equals(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetHashCode(byte[] x)
        {
            Debug.Assert(x != null);
            return Hash.GetFNVHashCode(x);
        }

        public static int GetHashCode(ImmutableArray<byte> x)
        {
            Debug.Assert(!x.IsDefault);
            return Hash.GetFNVHashCode(x);
        }

        bool IEqualityComparer<byte[]>.Equals(byte[] x, byte[] y)
        {
            return Equals(x, y);
        }

        int IEqualityComparer<byte[]>.GetHashCode(byte[] obj)
        {
            return GetHashCode(obj);
        }

        bool IEqualityComparer<ImmutableArray<byte>>.Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return Equals(x, y);
        }

        int IEqualityComparer<ImmutableArray<byte>>.GetHashCode(ImmutableArray<byte> obj)
        {
            return GetHashCode(obj);
        }
    }
}
