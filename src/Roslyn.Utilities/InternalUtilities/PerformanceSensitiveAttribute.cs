using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    [Conditional(conditionString: "EMIT_CODE_ANALYSIS_ATTRIBUTES")]
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = true,
        Inherited = false)]
    public sealed class PerformanceSensitiveAttribute : Attribute
    {
        public PerformanceSensitiveAttribute(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; }

        public string Constraint { get; set; }

        public bool AllowCaptures { get; set; }

        public bool AllowGenericEnumeration { get; set; }

        public bool AllowLocks { get; set; }

        public bool OftenCompletesSynchronously { get; set; }

        public bool IsParallelEntry { get; set; }
    }
}
