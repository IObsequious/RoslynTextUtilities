using System;

namespace Roslyn.Utilities
{
    public static class WeakReferenceExtensions
    {
        public static T GetTarget<T>(this WeakReference<T> reference) where T : class
        {
            reference.TryGetTarget(out T target);
            return target;
        }

        public static bool IsNull<T>(this WeakReference<T> reference) where T : class
        {
            return !reference.TryGetTarget(out T target);
        }
    }
}
