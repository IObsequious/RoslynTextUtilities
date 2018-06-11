using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(), nq}")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class DiagnosticBag
    {
        private ConcurrentQueue<Diagnostic> _lazyBag;

        public bool IsEmptyWithoutResolution
        {
            get
            {
                ConcurrentQueue<Diagnostic> bag = _lazyBag;
                return bag?.IsEmpty != false;
            }
        }

        public bool HasAnyErrors()
        {
            if (IsEmptyWithoutResolution)
            {
                return false;
            }

            foreach (Diagnostic diagnostic in Bag)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyResolvedErrors()
        {
            if (IsEmptyWithoutResolution)
            {
                return false;
            }

            foreach (Diagnostic diagnostic in Bag)
            {
                if ((diagnostic as DiagnosticWithInfo)?.HasLazyInfo != true && diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(Diagnostic diag)
        {
            ConcurrentQueue<Diagnostic> bag = Bag;
            bag.Enqueue(diag);
        }

        public void AddRange<T>(ImmutableArray<T> diagnostics) where T : Diagnostic
        {
            if (!diagnostics.IsDefaultOrEmpty)
            {
                ConcurrentQueue<Diagnostic> bag = Bag;
                for (int i = 0; i < diagnostics.Length; i++)
                {
                    bag.Enqueue(diagnostics[i]);
                }
            }
        }

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                Bag.Enqueue(diagnostic);
            }
        }

        public void AddRange(DiagnosticBag bag)
        {
            if (!bag.IsEmptyWithoutResolution)
            {
                AddRange(bag.Bag);
            }
        }

        public void AddRangeAndFree(DiagnosticBag bag)
        {
            AddRange(bag);
            bag.Free();
        }

        public ImmutableArray<TDiagnostic> ToReadOnlyAndFree<TDiagnostic>() where TDiagnostic : Diagnostic
        {
            ConcurrentQueue<Diagnostic> oldBag = _lazyBag;
            Free();
            return ToReadOnlyCore<TDiagnostic>(oldBag);
        }

        public ImmutableArray<Diagnostic> ToReadOnlyAndFree()
        {
            return ToReadOnlyAndFree<Diagnostic>();
        }

        public ImmutableArray<TDiagnostic> ToReadOnly<TDiagnostic>() where TDiagnostic : Diagnostic
        {
            ConcurrentQueue<Diagnostic> oldBag = _lazyBag;
            return ToReadOnlyCore<TDiagnostic>(oldBag);
        }

        public ImmutableArray<Diagnostic> ToReadOnly()
        {
            return ToReadOnly<Diagnostic>();
        }

        private static ImmutableArray<TDiagnostic> ToReadOnlyCore<TDiagnostic>(ConcurrentQueue<Diagnostic> oldBag)
            where TDiagnostic : Diagnostic
        {
            if (oldBag == null)
            {
                return ImmutableArray<TDiagnostic>.Empty;
            }

            ArrayBuilder<TDiagnostic> builder = ArrayBuilder<TDiagnostic>.GetInstance();
            foreach (TDiagnostic diagnostic in oldBag)
            {
                if (diagnostic.Severity != InternalDiagnosticSeverity.Void)
                {
                    Debug.Assert(diagnostic.Severity != InternalDiagnosticSeverity.Unknown);
                    builder.Add(diagnostic);
                }
            }

            return builder.ToImmutableAndFree();
        }

        public IEnumerable<Diagnostic> AsEnumerable()
        {
            ConcurrentQueue<Diagnostic> bag = Bag;
            bool foundVoid = false;
            foreach (Diagnostic diagnostic in bag)
            {
                if (diagnostic.Severity == InternalDiagnosticSeverity.Void)
                {
                    foundVoid = true;
                    break;
                }
            }

            return foundVoid ? AsEnumerableFiltered() : bag;
        }

        private IEnumerable<Diagnostic> AsEnumerableFiltered()
        {
            foreach (Diagnostic diagnostic in Bag)
            {
                if (diagnostic.Severity != InternalDiagnosticSeverity.Void)
                {
                    Debug.Assert(diagnostic.Severity != InternalDiagnosticSeverity.Unknown);
                    yield return diagnostic;
                }
            }
        }

        public IEnumerable<Diagnostic> AsEnumerableWithoutResolution()
        {
            return _lazyBag ?? SpecializedCollections.EmptyEnumerable<Diagnostic>();
        }

        public override string ToString()
        {
            if (IsEmptyWithoutResolution)
            {
                return "<no errors>";
            }

            StringBuilder builder = new StringBuilder();
            foreach (Diagnostic diag in Bag)
            {
                builder.AppendLine(diag.ToString());
            }

            return builder.ToString();
        }

        private ConcurrentQueue<Diagnostic> Bag
        {
            get
            {
                ConcurrentQueue<Diagnostic> bag = _lazyBag;
                if (bag != null)
                {
                    return bag;
                }

                ConcurrentQueue<Diagnostic> newBag = new ConcurrentQueue<Diagnostic>();
                return Interlocked.CompareExchange(ref _lazyBag, newBag, null) ?? newBag;
            }
        }

        public void Clear()
        {
            ConcurrentQueue<Diagnostic> bag = _lazyBag;
            if (bag != null)
            {
                _lazyBag = null;
            }
        }

        #region "Poolable"

        public static DiagnosticBag GetInstance()
        {
            DiagnosticBag bag = s_poolInstance.Allocate();
            return bag;
        }

        public void Free()
        {
            Clear();
            s_poolInstance.Free(this);
        }

        private static readonly ObjectPool<DiagnosticBag> s_poolInstance = CreatePool(128);

        private static ObjectPool<DiagnosticBag> CreatePool(int size)
        {
            return new ObjectPool<DiagnosticBag>(() => new DiagnosticBag(), size);
        }

        #endregion

        #region Debugger View

        public sealed class DebuggerProxy
        {
            private readonly DiagnosticBag _bag;

            public DebuggerProxy(DiagnosticBag bag)
            {
                _bag = bag;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object[] Diagnostics
            {
                get
                {
                    ConcurrentQueue<Diagnostic> lazyBag = _bag._lazyBag;
                    if (lazyBag != null)
                    {
                        return lazyBag.ToArray();
                    }

                    return Array.Empty<object>();
                }
            }
        }

        private string GetDebuggerDisplay()
        {
            return "Count = " + (_lazyBag?.Count ?? 0);
        }

        #endregion
    }
}
