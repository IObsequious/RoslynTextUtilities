using System.Collections.Generic;

namespace Roslyn.Utilities
{
    public sealed class StringOrdinalComparer : IEqualityComparer<string>
    {
        public static readonly StringOrdinalComparer Instance = new StringOrdinalComparer();

        private StringOrdinalComparer()
        {
        }

        bool IEqualityComparer<string>.Equals(string x, string y)
        {
            return Equals(x, y);
        }

        public static bool Equals(string a, string b)
        {
            if (b == null)
                throw new System.ArgumentNullException(nameof(b));

            if (a == null)
                throw new System.ArgumentNullException(nameof(a));

            return string.Equals(a, b);
        }

        int IEqualityComparer<string>.GetHashCode(string obj)
        {
            return Hash.GetFNVHashCode(obj);
        }
    }
}
