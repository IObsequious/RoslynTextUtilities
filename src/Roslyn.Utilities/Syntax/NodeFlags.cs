
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{


    [Flags]
    public enum NodeFlags : byte
    {
        None = 0,
        ContainsDiagnostics = 1 << 0,
        ContainsStructuredTrivia = 1 << 1,
        ContainsDirectives = 1 << 2,
        ContainsSkippedText = 1 << 3,
        ContainsAnnotations = 1 << 4,
        IsNotMissing = 1 << 5,
        InheritMask = ContainsDiagnostics | ContainsStructuredTrivia | ContainsDirectives | ContainsSkippedText | ContainsAnnotations | IsNotMissing,
    }
}
