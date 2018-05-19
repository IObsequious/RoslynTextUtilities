using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    public sealed class EmptyComparer : IEqualityComparer<object>
    {
        public static readonly EmptyComparer Instance = new EmptyComparer();

        private EmptyComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            Debug.Assert(false, message: "Are we using empty comparer with nonempty dictionary?");
            return false;
        }

        int IEqualityComparer<object>.GetHashCode(object s)
        {
            return 0;
        }
    }
}
