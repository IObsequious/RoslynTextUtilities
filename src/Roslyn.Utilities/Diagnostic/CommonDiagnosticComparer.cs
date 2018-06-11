using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class CommonDiagnosticComparer : IEqualityComparer<Diagnostic>
    {
        internal static readonly CommonDiagnosticComparer Instance = new CommonDiagnosticComparer();

        private CommonDiagnosticComparer()
        {
        }

        public bool Equals(Diagnostic x, Diagnostic y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Location == y.Location && x.Id == y.Id;
        }

        public int GetHashCode(Diagnostic obj)
        {
            if (obj is null)
            {
                return 0;
            }

            return Hash.Combine(obj.Location, obj.Id.GetHashCode());
        }
    }
}
