using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public static class FailFast
    {
        [DebuggerHidden]
        public static void OnFatalException(Exception exception)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#if !NETFX20
            AggregateException aggregate = exception as AggregateException;
            if (aggregate != null && aggregate.InnerExceptions.Count == 1)
            {
                exception = aggregate.InnerExceptions[0];
            }
#endif
            Environment.FailFast(exception.ToString(), exception);
        }

        [Conditional(conditionString: "DEBUG")]
        [DebuggerHidden]
        public static void Assert(bool condition, string message = null)
        {
            if (condition)
            {
                return;
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Environment.FailFast("ASSERT FAILED" + Environment.NewLine + message);
        }
    }
}
