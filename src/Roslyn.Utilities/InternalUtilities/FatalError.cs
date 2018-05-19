using System;
using System.Diagnostics;

#if COMPILERCORE
namespace Microsoft.CodeAnalysis
#else

namespace Microsoft.CodeAnalysis.ErrorReporting
#endif
{
    public static class FatalError
    {
        private static Action<Exception> s_fatalHandler;
        private static Action<Exception> s_nonFatalHandler;
        private static Exception s_reportedException;
        private static string s_reportedExceptionMessage;

        public static Action<Exception> Handler
        {
            get
            {
                return s_fatalHandler;
            }
            set
            {
                if (s_fatalHandler != value)
                {
                    Debug.Assert(s_fatalHandler == null, message: "Handler already set");
                    s_fatalHandler = value;
                }
            }
        }

        public static Action<Exception> NonFatalHandler
        {
            get
            {
                return s_nonFatalHandler;
            }
            set
            {
                if (s_nonFatalHandler != value)
                {
                    Debug.Assert(s_nonFatalHandler == null, message: "Handler already set");
                    s_nonFatalHandler = value;
                }
            }
        }

        public static void OverwriteHandler(Action<Exception> value)
        {
            s_fatalHandler = value;
        }

        [DebuggerHidden]
        public static bool ReportUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return Report(exception);
        }

        [DebuggerHidden]
        public static bool ReportWithoutCrashUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return ReportWithoutCrash(exception);
        }

        [DebuggerHidden]
        public static bool ReportUnlessNotImplemented(Exception exception)
        {
            if (exception is NotImplementedException)
            {
                return false;
            }

            return Report(exception);
        }

        [DebuggerHidden]
        public static bool Report(Exception exception)
        {
            Report(exception, s_fatalHandler);
            return false;
        }

        [DebuggerHidden]
        public static bool ReportWithoutCrash(Exception exception)
        {
            Report(exception, s_nonFatalHandler);
            return true;
        }

        private static void Report(Exception exception, Action<Exception> handler)
        {
            s_reportedException = exception;
            s_reportedExceptionMessage = exception.ToString();
            handler?.Invoke(exception);
        }
    }
}
