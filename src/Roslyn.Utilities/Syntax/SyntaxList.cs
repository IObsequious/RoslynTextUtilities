
using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class SyntaxList : SyntaxNode
    {
        public SyntaxList()
            : base(ListKind)
        {
        }

        public SyntaxList(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
            : base(ListKind, diagnostics, annotations)
        {
        }

        public SyntaxList(ObjectReader reader)
            : base(reader)
        {
        }

        public static SyntaxNode List(SyntaxNode child)
        {
            return child;
        }

        public static WithTwoChildren List(SyntaxNode child0, SyntaxNode child1)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);
            int hash;
            SyntaxNode cached = SyntaxNodeCache.TryGetNode(ListKind, child0, child1, out hash);
            if (cached != null)
                return (WithTwoChildren)cached;
            var result = new WithTwoChildren(child0, child1);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }
            return result;
        }

        public static WithThreeChildren List(SyntaxNode child0, SyntaxNode child1, SyntaxNode child2)
        {
            Debug.Assert(child0 != null);
            Debug.Assert(child1 != null);
            Debug.Assert(child2 != null);
            SyntaxNode cached = SyntaxNodeCache.TryGetNode(ListKind, child0, child1, child2, out int hash);
            if (cached != null)
                return (WithThreeChildren)cached;
            var result = new WithThreeChildren(child0, child1, child2);
            if (hash >= 0)
            {
                SyntaxNodeCache.AddNode(result, hash);
            }
            return result;
        }

        public static SyntaxNode List(SyntaxNode[] nodes)
        {
            return List(nodes, nodes.Length);
        }

        public static SyntaxNode List(SyntaxNode[] nodes, int count)
        {
            var array = new ArrayElement<SyntaxNode>[count];
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(nodes[i] != null);
                array[i].Value = nodes[i];
            }
            return List(array);
        }

        public static SyntaxList List(ArrayElement<SyntaxNode>[] children)
        {
            if (children.Length < 10)
            {
                return new WithManyChildren(children);
            }
            else
            {
                return new WithLotsOfChildren(children);
            }
        }

        public abstract void CopyTo(ArrayElement<SyntaxNode>[] array, int offset);

        public static SyntaxNode Concat(SyntaxNode left, SyntaxNode right)
        {
            if (left == null)
            {
                return right;
            }
            if (right == null)
            {
                return left;
            }
            var leftList = left as SyntaxList;
            var rightList = right as SyntaxList;
            if (leftList != null)
            {
                if (rightList != null)
                {
                    var tmp = new ArrayElement<SyntaxNode>[left.SlotCount + right.SlotCount];
                    leftList.CopyTo(tmp, 0);
                    rightList.CopyTo(tmp, left.SlotCount);
                    return List(tmp);
                }
                else
                {
                    var tmp = new ArrayElement<SyntaxNode>[left.SlotCount + 1];
                    leftList.CopyTo(tmp, 0);
                    tmp[left.SlotCount].Value = right;
                    return List(tmp);
                }
            }
            else if (rightList != null)
            {
                var tmp = new ArrayElement<SyntaxNode>[rightList.SlotCount + 1];
                tmp[0].Value = left;
                rightList.CopyTo(tmp, 1);
                return List(tmp);
            }
            else
            {
                return List(left, right);
            }
        }

        public sealed override string Language
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public sealed override string KindText
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        //public sealed override SyntaxNode GetStructure(SyntaxTrivia parentTrivia)
        //{
        //    throw ExceptionUtilities.Unreachable;
        //}

        //public sealed override SyntaxToken CreateSeparator<TNode>(SyntaxNode element)
        //{
        //    throw ExceptionUtilities.Unreachable;
        //}

        public sealed override bool IsTriviaWithEndOfLine()
        {
            return false;
        }

        public class WithTwoChildren : SyntaxList
        {

            static WithTwoChildren()
            {
                ObjectBinder.RegisterTypeReader(typeof(WithTwoChildren), r => new WithTwoChildren(r));
            }
            private readonly SyntaxNode _child0;
            private readonly SyntaxNode _child1;

            public WithTwoChildren(SyntaxNode child0, SyntaxNode child1)
            {
                SlotCount = 2;
                AdjustFlagsAndWidth(child0);
                _child0 = child0;
                AdjustFlagsAndWidth(child1);
                _child1 = child1;
            }

            public WithTwoChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, SyntaxNode child0, SyntaxNode child1)
                : base(diagnostics, annotations)
            {
                SlotCount = 2;
                AdjustFlagsAndWidth(child0);
                _child0 = child0;
                AdjustFlagsAndWidth(child1);
                _child1 = child1;
            }

            public WithTwoChildren(ObjectReader reader)
                : base(reader)
            {
                SlotCount = 2;
                _child0 = (SyntaxNode)reader.ReadValue();
                AdjustFlagsAndWidth(_child0);
                _child1 = (SyntaxNode)reader.ReadValue();
                AdjustFlagsAndWidth(_child1);
            }

            public override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_child0);
                writer.WriteValue(_child1);
            }

            public override SyntaxNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    default:
                        return null;
                }
            }

            public override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
            }

            //public override SyntaxNode CreateRed(SyntaxNode parent, int position)
            //{
            //    return new Syntax.SyntaxList.WithTwoChildren(this, parent, position);
            //}

            public override SyntaxNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithTwoChildren(errors, GetAnnotations(), _child0, _child1);
            }

            public override SyntaxNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithTwoChildren(GetDiagnostics(), annotations, _child0, _child1);
            }
        }

        public class WithThreeChildren : SyntaxList
        {

            static WithThreeChildren()
            {
                ObjectBinder.RegisterTypeReader(typeof(WithThreeChildren), r => new WithThreeChildren(r));
            }
            private readonly SyntaxNode _child0;
            private readonly SyntaxNode _child1;
            private readonly SyntaxNode _child2;

            public WithThreeChildren(SyntaxNode child0, SyntaxNode child1, SyntaxNode child2)
            {
                SlotCount = 3;
                AdjustFlagsAndWidth(child0);
                _child0 = child0;
                AdjustFlagsAndWidth(child1);
                _child1 = child1;
                AdjustFlagsAndWidth(child2);
                _child2 = child2;
            }

            public WithThreeChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, SyntaxNode child0, SyntaxNode child1, SyntaxNode child2)
                : base(diagnostics, annotations)
            {
                SlotCount = 3;
                AdjustFlagsAndWidth(child0);
                _child0 = child0;
                AdjustFlagsAndWidth(child1);
                _child1 = child1;
                AdjustFlagsAndWidth(child2);
                _child2 = child2;
            }

            public WithThreeChildren(ObjectReader reader)
                : base(reader)
            {
                SlotCount = 3;
                _child0 = (SyntaxNode)reader.ReadValue();
                AdjustFlagsAndWidth(_child0);
                _child1 = (SyntaxNode)reader.ReadValue();
                AdjustFlagsAndWidth(_child1);
                _child2 = (SyntaxNode)reader.ReadValue();
                AdjustFlagsAndWidth(_child2);
            }

            public override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_child0);
                writer.WriteValue(_child1);
                writer.WriteValue(_child2);
            }

            public override SyntaxNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    case 2:
                        return _child2;
                    default:
                        return null;
                }
            }

            public override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
                array[offset + 2].Value = _child2;
            }

            //public override SyntaxNode CreateRed(SyntaxNode parent, int position)
            //{
            //    return new Syntax.SyntaxList.WithThreeChildren(this, parent, position);
            //}

            public override SyntaxNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithThreeChildren(errors, GetAnnotations(), _child0, _child1, _child2);
            }

            public override SyntaxNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithThreeChildren(GetDiagnostics(), annotations, _child0, _child1, _child2);
            }
        }

        public abstract class WithManyChildrenBase : SyntaxList
        {
            internal readonly ArrayElement<SyntaxNode>[] children;

            public WithManyChildrenBase(ArrayElement<SyntaxNode>[] children)
            {
                this.children = children;
                InitializeChildren();
            }

            public WithManyChildrenBase(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<SyntaxNode>[] children)
                : base(diagnostics, annotations)
            {
                this.children = children;
                InitializeChildren();
            }

            private void InitializeChildren()
            {
                int n = children.Length;
                if (n < byte.MaxValue)
                {
                    SlotCount = (byte)n;
                }
                else
                {
                    SlotCount = byte.MaxValue;
                }
                for (int i = 0; i < children.Length; i++)
                {
                    AdjustFlagsAndWidth(children[i]);
                }
            }

            public WithManyChildrenBase(ObjectReader reader)
                : base(reader)
            {
                var length = reader.ReadInt32();
                children = new ArrayElement<SyntaxNode>[length];
                for (var i = 0; i < length; i++)
                {
                    children[i].Value = (SyntaxNode)reader.ReadValue();
                }
                InitializeChildren();
            }

            public override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteInt32(children.Length);
                for (var i = 0; i < children.Length; i++)
                {
                    writer.WriteValue(children[i].Value);
                }
            }

            protected override int GetSlotCount()
            {
                return children.Length;
            }

            public override SyntaxNode GetSlot(int index)
            {
                return children[index];
            }

            public override void CopyTo(ArrayElement<SyntaxNode>[] array, int offset)
            {
                Array.Copy(children, 0, array, offset, children.Length);
            }

            //public override SyntaxNode CreateRed(SyntaxNode parent, int position)
            //{
            //    var separated = this.SlotCount > 1 && HasNodeTokenPattern();
            //    if (parent != null && parent.ShouldCreateWeakList())
            //    {
            //        return separated
            //            ? new Syntax.SyntaxList.SeparatedWithManyWeakChildren(this, parent, position)
            //            : (SyntaxNode)new SyntaxList.WithManyWeakChildren(this, parent, position);
            //    }
            //    else
            //    {
            //        return separated
            //            ? new Syntax.SyntaxList.SeparatedWithManyChildren(this, parent, position)
            //            : (SyntaxNode)new Syntax.SyntaxList.WithManyChildren(this, parent, position);
            //    }
            //}

            private bool HasNodeTokenPattern()
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (GetSlot(i).IsToken == ((i & 1) == 0))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public sealed class WithManyChildren : WithManyChildrenBase
        {

            static WithManyChildren()
            {
                ObjectBinder.RegisterTypeReader(typeof(WithManyChildren), r => new WithManyChildren(r));
            }

            public WithManyChildren(ArrayElement<SyntaxNode>[] children)
                : base(children)
            {
            }

            public WithManyChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<SyntaxNode>[] children)
                : base(diagnostics, annotations, children)
            {
            }

            public WithManyChildren(ObjectReader reader)
                : base(reader)
            {
            }

            public override SyntaxNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithManyChildren(errors, GetAnnotations(), children);
            }

            public override SyntaxNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithManyChildren(GetDiagnostics(), annotations, children);
            }
        }

        public sealed class WithLotsOfChildren : WithManyChildrenBase
        {

            static WithLotsOfChildren()
            {
                ObjectBinder.RegisterTypeReader(typeof(WithLotsOfChildren), r => new WithLotsOfChildren(r));
            }
            private readonly int[] _childOffsets;

            public WithLotsOfChildren(ArrayElement<SyntaxNode>[] children)
                : base(children)
            {
                _childOffsets = CalculateOffsets(children);
            }

            public WithLotsOfChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<SyntaxNode>[] children, int[] childOffsets)
                : base(diagnostics, annotations, children)
            {
                _childOffsets = childOffsets;
            }

            public WithLotsOfChildren(ObjectReader reader)
                : base(reader)
            {
                _childOffsets = CalculateOffsets(children);
            }

            public override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
            }

            public override int GetSlotOffset(int index)
            {
                return _childOffsets[index];
            }

            public override int FindSlotIndexContainingOffset(int offset)
            {
                Debug.Assert(offset >= 0 && offset < FullWidth);
                return _childOffsets.BinarySearchUpperBound(offset) - 1;
            }

            private static int[] CalculateOffsets(ArrayElement<SyntaxNode>[] children)
            {
                int n = children.Length;
                var childOffsets = new int[n];
                int offset = 0;
                for (int i = 0; i < n; i++)
                {
                    childOffsets[i] = offset;
                    offset += children[i].Value.FullWidth;
                }
                return childOffsets;
            }

            public override SyntaxNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithLotsOfChildren(errors, GetAnnotations(), children, _childOffsets);
            }

            public override SyntaxNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithLotsOfChildren(GetDiagnostics(), annotations, children, _childOffsets);
            }
        }
    }
}
