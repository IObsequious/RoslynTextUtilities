using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities
{
    public static class ImmutableByteArrayInterop
    {
        public static byte[] DangerousGetUnderlyingArray(this ImmutableArray<byte> array)
        {
            ArrayUnion union = new ArrayUnion();
            union.ImmutableArray = array;
            return union.MutableArray;
        }

        public static ImmutableArray<byte> DangerousCreateFromUnderlyingArray(ref byte[] array)
        {
            ArrayUnion union = new ArrayUnion();
            union.MutableArray = array;
            array = null;
            return union.ImmutableArray;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ArrayUnion
        {
            [FieldOffset(0)] internal byte[] MutableArray;
            [FieldOffset(0)] internal ImmutableArray<byte> ImmutableArray;
        }
    }

    public static class ImmutableInt32ArrayInterop
    {
        public static int[] DangerousGetUnderlyingArray(this ImmutableArray<int> array)
        {
            ArrayUnion union = new ArrayUnion();
            union.ImmutableArray = array;
            return union.MutableArray;
        }

        public static ImmutableArray<int> DangerousCreateFromUnderlyingArray(ref int[] array)
        {
            ArrayUnion union = new ArrayUnion();
            union.MutableArray = array;
            array = null;
            return union.ImmutableArray;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ArrayUnion
        {
            [FieldOffset(0)] internal int[] MutableArray;
            [FieldOffset(0)] internal ImmutableArray<int> ImmutableArray;
        }
    }
}
