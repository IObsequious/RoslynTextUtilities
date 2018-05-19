using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    public class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object a, object b)
        {
            return a == b;
        }

        int IEqualityComparer<object>.GetHashCode(object a)
        {
            return GetHashCode(a);
        }

        public static int GetHashCode(object a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
}
