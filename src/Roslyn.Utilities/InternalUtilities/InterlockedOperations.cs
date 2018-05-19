using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Roslyn.Utilities
{
    public static class InterlockedOperations
    {
        public static T Initialize<T>(ref T target, T value) where T : class
        {
            Debug.Assert(value != null);
            return Interlocked.CompareExchange(ref target, value, null) ?? value;
        }

        public static T Initialize<T>(ref T target, T initializedValue, T uninitializedValue) where T : class
        {
            Debug.Assert(initializedValue != uninitializedValue);
            T oldValue = Interlocked.CompareExchange(ref target, initializedValue, uninitializedValue);
            return (object) oldValue == uninitializedValue ? initializedValue : oldValue;
        }

        public static ImmutableArray<T> Initialize<T>(ref ImmutableArray<T> target, ImmutableArray<T> initializedValue)
        {
            Debug.Assert(!initializedValue.IsDefault);
            var oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref target, initializedValue, default(ImmutableArray<T>));
            return oldValue.IsDefault ? initializedValue : oldValue;
        }
    }
}
