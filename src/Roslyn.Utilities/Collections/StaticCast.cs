using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public static class StaticCast<T>
    {
        public static ImmutableArray<T> From<TDerived>(ImmutableArray<TDerived> from) where TDerived : class, T
        {
            return ImmutableArray<T>.CastUp(from);
        }
    }
}
