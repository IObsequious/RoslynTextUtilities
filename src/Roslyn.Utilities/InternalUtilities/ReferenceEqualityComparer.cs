using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return x == y;
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return GetHashCode(obj);
        }

        public static int GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
}
