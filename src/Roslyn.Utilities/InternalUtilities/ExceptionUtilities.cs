using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    public static class ExceptionUtilities
    {
        public static Exception UnexpectedValue(object o)
        {
            string output = string.Format(format: "Unexpected value '{0}' of type '{1}'",
                arg0: o,
                arg1: o != null ? o.GetType().FullName : "<unknown>");
            Debug.Fail(output);
            return new InvalidOperationException(output);
        }

        public static Exception Unreachable
        {
            get
            {
                return new InvalidOperationException(message: "This program location is thought to be unreachable.");
            }
        }
    }
}
