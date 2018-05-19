namespace Roslyn.Utilities
{
    public static class DecimalUtilities
    {
        public static int GetScale(this decimal value)
        {
            return unchecked((byte) (decimal.GetBits(value)[3] >> 16));
        }

        public static void GetBits(this decimal value, out bool isNegative, out byte scale, out uint low, out uint mid, out uint high)
        {
            int[] bits = decimal.GetBits(value);
            low = unchecked((uint) bits[0]);
            mid = unchecked((uint) bits[1]);
            high = unchecked((uint) bits[2]);
            scale = unchecked((byte) (bits[3] >> 16));
            isNegative = (bits[3] & 0x80000000) != 0;
        }
    }
}
