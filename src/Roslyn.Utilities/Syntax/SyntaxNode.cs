
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

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract class SyntaxNode : IObjectWritable
    {

        private string GetDebuggerDisplay()
        {
            return GetType().Name + " " + KindText + " " + ToString();
        }
        internal const int ListKind = 1;
        private readonly ushort _kind;
        protected NodeFlags flags;
        private byte _slotCount;
        private int _fullWidth;
        private static readonly ConditionalWeakTable<SyntaxNode, DiagnosticInfo[]> s_diagnosticsTable =
            new ConditionalWeakTable<SyntaxNode, DiagnosticInfo[]>();
        private static readonly ConditionalWeakTable<SyntaxNode, SyntaxAnnotation[]> s_annotationsTable =
            new ConditionalWeakTable<SyntaxNode, SyntaxAnnotation[]>();
        private static readonly DiagnosticInfo[] s_noDiagnostics = Array.Empty<DiagnosticInfo>();
        private static readonly SyntaxAnnotation[] s_noAnnotations = Array.Empty<SyntaxAnnotation>();
        private static readonly IEnumerable<SyntaxAnnotation> s_noAnnotationsEnumerable = SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();

        protected SyntaxNode(ushort kind)
        {
            _kind = kind;
        }

        protected SyntaxNode(ushort kind, int fullWidth)
        {
            _kind = kind;
            _fullWidth = fullWidth;
        }

        protected SyntaxNode(ushort kind, DiagnosticInfo[] diagnostics, int fullWidth)
        {
            _kind = kind;
            _fullWidth = fullWidth;
            if (diagnostics?.Length > 0)
            {
                flags |= NodeFlags.ContainsDiagnostics;
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected SyntaxNode(ushort kind, DiagnosticInfo[] diagnostics)
        {
            _kind = kind;
            if (diagnostics?.Length > 0)
            {
                flags |= NodeFlags.ContainsDiagnostics;
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected SyntaxNode(ushort kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations) :
                    this(kind, diagnostics)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null)
                        throw new ArgumentException(paramName: nameof(annotations), message: "");
                }
                flags |= NodeFlags.ContainsAnnotations;
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected SyntaxNode(ushort kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, int fullWidth) :
                    this(kind, diagnostics, fullWidth)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null)
                        throw new ArgumentException(paramName: nameof(annotations), message: "");
                }
                flags |= NodeFlags.ContainsAnnotations;
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected void AdjustFlagsAndWidth(SyntaxNode node)
        {
            flags |= (node.flags & NodeFlags.InheritMask);
            _fullWidth += node._fullWidth;
        }

        public abstract string Language
        {
            get;
        }
        #region Kind 

        public int RawKind
        {
            get
            {
                return _kind;
            }
        }

        public bool IsList
        {
            get
            {
                return RawKind == ListKind;
            }
        }

        public abstract string KindText
        {
            get;
        }

        public virtual bool IsStructuredTrivia => false;

        public virtual bool IsDirective => false;

        public virtual bool IsToken => false;

        public virtual bool IsTrivia => false;

        public virtual bool IsSkippedTokensTrivia => false;

        public virtual bool IsDocumentationCommentTrivia => false;
        #endregion
        #region Slots 

        public int SlotCount
        {
            get
            {
                int count = _slotCount;
                if (count == byte.MaxValue)
                {
                    count = GetSlotCount();
                }
                return count;
            }
            protected set
            {
                _slotCount = (byte)value;
            }
        }

        public abstract SyntaxNode GetSlot(int index);

        protected virtual int GetSlotCount()
        {
            return _slotCount;
        }

        public virtual int GetSlotOffset(int index)
        {
            int offset = 0;
            for (int i = 0; i < index; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                {
                    offset += child.FullWidth;
                }
            }
            return offset;
        }

        public ChildSyntaxList ChildNodesAndTokens()
        {
            return new ChildSyntaxList(this);
        }

        public IEnumerable<SyntaxNode> EnumerateNodes()
        {
            yield return this;
            var stack = new Stack<ChildSyntaxList.Enumerator>(24);
            stack.Push(ChildNodesAndTokens().GetEnumerator());
            while (stack.Count > 0)
            {
                var en = stack.Pop();
                if (!en.MoveNext())
                {
                    continue;
                }
                var current = en.Current;
                stack.Push(en);
                yield return current;
                if (!current.IsToken)
                {
                    stack.Push(current.ChildNodesAndTokens().GetEnumerator());
                    continue;
                }
            }
        }

        public virtual int FindSlotIndexContainingOffset(int offset)
        {
            Debug.Assert(0 <= offset && offset < FullWidth);
            int i;
            int accumulatedWidth = 0;
            for (i = 0; ; i++)
            {
                Debug.Assert(i < SlotCount);
                var child = GetSlot(i);
                if (child != null)
                {
                    accumulatedWidth += child.FullWidth;
                    if (offset < accumulatedWidth)
                    {
                        break;
                    }
                }
            }
            return i;
        }
        #endregion
        #region Flags 
        public NodeFlags Flags
        {
            get
            {
                return flags;
            }
        }

        public void SetFlags(NodeFlags flags)
        {
            this.flags |= flags;
        }

        public void ClearFlags(NodeFlags flags)
        {
            this.flags &= ~flags;
        }

        public bool IsMissing
        {
            get
            {
                return (flags & NodeFlags.IsNotMissing) == 0;
            }
        }

        public bool ContainsSkippedText
        {
            get
            {
                return (flags & NodeFlags.ContainsSkippedText) != 0;
            }
        }

        public bool ContainsStructuredTrivia
        {
            get
            {
                return (flags & NodeFlags.ContainsStructuredTrivia) != 0;
            }
        }

        public bool ContainsDirectives
        {
            get
            {
                return (flags & NodeFlags.ContainsDirectives) != 0;
            }
        }

        public bool ContainsDiagnostics
        {
            get
            {
                return (flags & NodeFlags.ContainsDiagnostics) != 0;
            }
        }

        public bool ContainsAnnotations
        {
            get
            {
                return (flags & NodeFlags.ContainsAnnotations) != 0;
            }
        }
        #endregion
        #region Spans

        public int FullWidth
        {
            get
            {
                return _fullWidth;
            }
            protected set
            {
                _fullWidth = value;
            }
        }

        public virtual int Width
        {
            get
            {
                return _fullWidth - GetLeadingTriviaWidth() - GetTrailingTriviaWidth();
            }
        }

        public virtual int GetLeadingTriviaWidth()
        {
            return FullWidth != 0 ?
                GetFirstTerminal().GetLeadingTriviaWidth() :
                0;
        }

        public virtual int GetTrailingTriviaWidth()
        {
            return FullWidth != 0 ?
                GetLastTerminal().GetTrailingTriviaWidth() :
                0;
        }

        public bool HasLeadingTrivia
        {
            get
            {
                return GetLeadingTriviaWidth() != 0;
            }
        }

        public bool HasTrailingTrivia
        {
            get
            {
                return GetTrailingTriviaWidth() != 0;
            }
        }
        #endregion
        #region Serialization 
        private const ushort ExtendedSerializationInfoMask = unchecked((ushort)(1u << 15));

        public SyntaxNode(ObjectReader reader)
        {
            var kindBits = reader.ReadUInt16();
            _kind = (ushort)(kindBits & ~ExtendedSerializationInfoMask);
            if ((kindBits & ExtendedSerializationInfoMask) != 0)
            {
                var diagnostics = (DiagnosticInfo[])reader.ReadValue();
                if (diagnostics != null && diagnostics.Length > 0)
                {
                    flags |= NodeFlags.ContainsDiagnostics;
                    s_diagnosticsTable.Add(this, diagnostics);
                }
                var annotations = (SyntaxAnnotation[])reader.ReadValue();
                if (annotations != null && annotations.Length > 0)
                {
                    flags |= NodeFlags.ContainsAnnotations;
                    s_annotationsTable.Add(this, annotations);
                }
            }
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            WriteTo(writer);
        }

        public virtual void WriteTo(ObjectWriter writer)
        {
            var kindBits = (ushort)_kind;
            var hasDiagnostics = GetDiagnostics().Length > 0;
            var hasAnnotations = GetAnnotations().Length > 0;
            if (hasDiagnostics || hasAnnotations)
            {
                kindBits |= ExtendedSerializationInfoMask;
                writer.WriteUInt16(kindBits);
                writer.WriteValue(hasDiagnostics ? GetDiagnostics() : null);
                writer.WriteValue(hasAnnotations ? GetAnnotations() : null);
            }
            else
            {
                writer.WriteUInt16(kindBits);
            }
        }
        #endregion
        #region Annotations 

        public bool HasAnnotations(string annotationKind)
        {
            var annotations = GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }
            foreach (var a in annotations)
            {
                if (a.Kind == annotationKind)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            var annotations = GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }
            foreach (var a in annotations)
            {
                if (annotationKinds.Contains(a.Kind))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            var annotations = GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }
            foreach (var a in annotations)
            {
                if (a == annotation)
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            if (string.IsNullOrWhiteSpace(annotationKind))
            {
                throw new ArgumentNullException(nameof(annotationKind));
            }
            var annotations = GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }
            return GetAnnotationsSlow(annotations, annotationKind);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, string annotationKind)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Kind == annotationKind)
                {
                    yield return annotation;
                }
            }
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            if (annotationKinds == null)
            {
                throw new ArgumentNullException(nameof(annotationKinds));
            }
            var annotations = GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }
            return GetAnnotationsSlow(annotations, annotationKinds);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, IEnumerable<string> annotationKinds)
        {
            foreach (var annotation in annotations)
            {
                if (annotationKinds.Contains(annotation.Kind))
                {
                    yield return annotation;
                }
            }
        }

        public SyntaxAnnotation[] GetAnnotations()
        {
            if (ContainsAnnotations)
            {
                SyntaxAnnotation[] annotations;
                if (s_annotationsTable.TryGetValue(this, out annotations))
                {
                    Debug.Assert(annotations.Length != 0, "we should return nonempty annotations or NoAnnotations");
                    return annotations;
                }
            }
            return s_noAnnotations;
        }

        public abstract SyntaxNode SetAnnotations(SyntaxAnnotation[] annotations);
        #endregion
        #region Diagnostics

        public DiagnosticInfo[] GetDiagnostics()
        {
            if (ContainsDiagnostics)
            {
                DiagnosticInfo[] diags;
                if (s_diagnosticsTable.TryGetValue(this, out diags))
                {
                    return diags;
                }
            }
            return s_noDiagnostics;
        }

        public abstract SyntaxNode SetDiagnostics(DiagnosticInfo[] diagnostics);
        #endregion
        #region Text

        public virtual string ToFullString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            WriteTo(writer, leading: true, trailing: true);
            return sb.ToStringAndFree();
        }

        public override string ToString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            WriteTo(writer, leading: false, trailing: false);
            return sb.ToStringAndFree();
        }

        public void WriteTo(TextWriter writer, bool leading = true, bool trailing = true)
        {
            var stack = new Stack<(SyntaxNode node, bool leading, bool trailing)>();
            stack.Push((this, leading, trailing));
            ProcessStack(writer, stack);
        }

        private static void ProcessStack(TextWriter writer,
                    Stack<(SyntaxNode node, bool leading, bool trailing)> stack)
        {
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var currentNode = current.node;
                var currentLeading = current.leading;
                var currentTrailing = current.trailing;
                if (currentNode.IsToken)
                {
                    currentNode.WriteTokenTo(writer, currentLeading, currentTrailing);
                    continue;
                }
                if (currentNode.IsTrivia)
                {
                    currentNode.WriteTriviaTo(writer);
                    continue;
                }
                var firstIndex = GetFirstNonNullChildIndex(currentNode);
                var lastIndex = GetLastNonNullChildIndex(currentNode);
                for (var i = lastIndex; i >= firstIndex; i--)
                {
                    var child = currentNode.GetSlot(i);
                    if (child != null)
                    {
                        var first = i == firstIndex;
                        var last = i == lastIndex;
                        stack.Push((child, currentLeading | !first, currentTrailing | !last));
                    }
                }
            }
        }

        private static int GetFirstNonNullChildIndex(SyntaxNode node)
        {
            int n = node.SlotCount;
            int firstIndex = 0;
            for (; firstIndex < n; firstIndex++)
            {
                var child = node.GetSlot(firstIndex);
                if (child != null)
                {
                    break;
                }
            }
            return firstIndex;
        }

        private static int GetLastNonNullChildIndex(SyntaxNode node)
        {
            int n = node.SlotCount;
            int lastIndex = n - 1;
            for (; lastIndex >= 0; lastIndex--)
            {
                var child = node.GetSlot(lastIndex);
                if (child != null)
                {
                    break;
                }
            }
            return lastIndex;
        }

        protected virtual void WriteTriviaTo(TextWriter writer)
        {
            throw new NotImplementedException();
        }

        protected virtual void WriteTokenTo(TextWriter writer, bool leading, bool trailing)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region Tokens 

        public virtual int RawContextualKind
        {
            get
            {
                return RawKind;
            }
        }

        public virtual object GetValue()
        {
            return null;
        }

        public virtual string GetValueText()
        {
            return string.Empty;
        }

        public virtual SyntaxNode GetLeadingTriviaCore()
        {
            return null;
        }

        public virtual SyntaxNode GetTrailingTriviaCore()
        {
            return null;
        }

        public virtual SyntaxNode WithLeadingTrivia(SyntaxNode trivia)
        {
            return this;
        }

        public virtual SyntaxNode WithTrailingTrivia(SyntaxNode trivia)
        {
            return this;
        }

        public SyntaxNode GetFirstTerminal()
        {
            SyntaxNode node = this;
            do
            {
                SyntaxNode firstChild = null;
                for (int i = 0, n = node.SlotCount; i < n; i++)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        firstChild = child;
                        break;
                    }
                }
                node = firstChild;
            } while (node?._slotCount > 0);
            return node;
        }

        public SyntaxNode GetLastTerminal()
        {
            SyntaxNode node = this;
            do
            {
                SyntaxNode lastChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        lastChild = child;
                        break;
                    }
                }
                node = lastChild;
            } while (node?._slotCount > 0);
            return node;
        }

        public SyntaxNode GetLastNonmissingTerminal()
        {
            SyntaxNode node = this;
            do
            {
                SyntaxNode nonmissingChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null && !child.IsMissing)
                    {
                        nonmissingChild = child;
                        break;
                    }
                }
                node = nonmissingChild;
            }
            while (node?._slotCount > 0);
            return node;
        }
        #endregion
        #region Equivalence 

        public virtual bool IsEquivalentTo(SyntaxNode other)
        {
            if (this == other)
            {
                return true;
            }
            if (other == null)
            {
                return false;
            }
            return EquivalentToInternal(this, other);
        }

        private static bool EquivalentToInternal(SyntaxNode node1, SyntaxNode node2)
        {
            if (node1.RawKind != node2.RawKind)
            {
                if (node1.IsList && node1.SlotCount == 1)
                {
                    node1 = node1.GetSlot(0);
                }
                if (node2.IsList && node2.SlotCount == 1)
                {
                    node2 = node2.GetSlot(0);
                }
                if (node1.RawKind != node2.RawKind)
                {
                    return false;
                }
            }
            if (node1._fullWidth != node2._fullWidth)
            {
                return false;
            }
            var n = node1.SlotCount;
            if (n != node2.SlotCount)
            {
                return false;
            }
            for (int i = 0; i < n; i++)
            {
                var node1Child = node1.GetSlot(i);
                var node2Child = node2.GetSlot(i);
                if (node1Child != null && node2Child != null && !node1Child.IsEquivalentTo(node2Child))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
        #region Factories 

        public abstract bool IsTriviaWithEndOfLine();

        public virtual SyntaxNode CreateList(IEnumerable<SyntaxNode> nodes, bool alwaysCreateListNode = false)
        {
            if (nodes == null)
            {
                return null;
            }
            var list = nodes.ToArray();
            switch (list.Length)
            {
                case 0:
                    return null;
                case 1:
                    if (alwaysCreateListNode)
                    {
                        goto default;
                    }
                    else
                    {
                        return list[0];
                    }
                case 2:
                    return SyntaxList.List(list[0], list[1]);
                case 3:
                    return SyntaxList.List(list[0], list[1], list[2]);
                default:
                    return SyntaxList.List(list);
            }
        }
        #endregion
        #region Caching

        public const int MaxCachedChildNum = 3;

        public bool IsCacheable
        {
            get
            {
                return ((flags & NodeFlags.InheritMask) == NodeFlags.IsNotMissing) &&
                    SlotCount <= MaxCachedChildNum;
            }
        }

        public int GetCacheHash()
        {
            Debug.Assert(IsCacheable);
            int code = (int)(flags) ^ RawKind;
            int cnt = SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                {
                    code = Hash.Combine(RuntimeHelpers.GetHashCode(child), code);
                }
            }
            return code & int.MaxValue;
        }

        public bool IsCacheEquivalent(int kind, NodeFlags flags, SyntaxNode child1)
        {
            Debug.Assert(IsCacheable);
            return RawKind == kind &&
                this.flags == flags &&
                GetSlot(0) == child1;
        }

        public bool IsCacheEquivalent(int kind, NodeFlags flags, SyntaxNode child1, SyntaxNode child2)
        {
            Debug.Assert(IsCacheable);
            return RawKind == kind &&
                this.flags == flags &&
                GetSlot(0) == child1 &&
                GetSlot(1) == child2;
        }

        public bool IsCacheEquivalent(int kind, NodeFlags flags, SyntaxNode child1, SyntaxNode child2, SyntaxNode child3)
        {
            Debug.Assert(IsCacheable);
            return RawKind == kind &&
                this.flags == flags &&
                GetSlot(0) == child1 &&
                GetSlot(1) == child2 &&
                GetSlot(2) == child3;
        }
        #endregion

        public SyntaxNode AddError(DiagnosticInfo err)
        {
            DiagnosticInfo[] errorInfos;
            if (GetDiagnostics() == null)
            {
                errorInfos = new[] { err };
            }
            else
            {
                errorInfos = GetDiagnostics();
                var length = errorInfos.Length;
                Array.Resize(ref errorInfos, length + 1);
                errorInfos[length] = err;
            }
            return SetDiagnostics(errorInfos);
        }


        public SeparatedSyntaxList<T> ToSyntaxSeparatedList<T>() where T : SyntaxNode
        {
            return
                new SeparatedSyntaxList<T>(ToSyntaxList<T>());
        }

        public SyntaxList<T> ToSyntaxList<T>() where T : SyntaxNode
        {
            return new SyntaxList<T>(this);
        }
    }
}
