// -----------------------------------------------------------------------
// <copyright file="ExceptionUtilities.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;
using System.Diagnostics;

namespace System.Text
{
    internal static class ExceptionUtilities
    {
        public static Exception UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, o != null ? o.GetType().FullName : "<unknown>");

            Debug.Fail(output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        public static Exception Unreachable
        {
            get
            {
                return new InvalidOperationException("This program location is thought to be unreachable.");
            }
        }
    }
}
