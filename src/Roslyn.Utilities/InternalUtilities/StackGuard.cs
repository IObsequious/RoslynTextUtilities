using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    public static class StackGuard
    {
        public const int MaxUncheckedRecursionDepth = 20;

        public static void EnsureSufficientExecutionStack(int recursionDepth)
        {
            if (recursionDepth > MaxUncheckedRecursionDepth)
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
        }

        public static bool IsInsufficientExecutionStackException(Exception ex)
        {
            return ex.GetType().Name == "InsufficientExecutionStackException";
        }
    }
}
