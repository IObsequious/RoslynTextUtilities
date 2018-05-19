
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{

    public partial struct SyntaxList<TNode> : IEquatable<SyntaxList<TNode>>, IEnumerable<TNode>, IReadOnlyList<TNode>
            where TNode : SyntaxNode
    {
        private readonly SyntaxNode _node;

        public SyntaxList(SyntaxNode node)
        {
            _node = node;
        }

        public SyntaxNode Node => _node;

        public int Count
        {
            get
            {
                return _node == null ? 0 : (_node.IsList ? _node.SlotCount : 1);
            }
        }
        public TNode this[int index]
        {
            get
            {
                if (_node == null)
                {
                    return null;
                }
                else if (_node.IsList)
                {
                    Debug.Assert(index >= 0);
                    Debug.Assert(index <= _node.SlotCount);
                    return (TNode)_node.GetSlot(index);
                }
                else if (index == 0)
                {
                    return (TNode)_node;
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        public SyntaxNode ItemUntyped(int index)
        {
            var node = _node;
            if (node.IsList)
            {
                return node.GetSlot(index);
            }
            Debug.Assert(index == 0);
            return node;
        }

        public bool Any()
        {
            return _node != null;
        }

        public bool Any(int kind)
        {
            foreach (var element in this)
            {
                if (element.RawKind == kind)
                {
                    return true;
                }
            }
            return false;
        }

        public TNode[] Nodes
        {
            get
            {
                var arr = new TNode[Count];
                for (int i = 0; i < Count; i++)
                {
                    arr[i] = this[i];
                }
                return arr;
            }
        }

        public List<TNode> ToList() => new List<TNode>(Nodes);

        public TNode[] ToArray() => Nodes;

        public TNode Last
        {
            get
            {
                var node = _node;
                if (node.IsList)
                {
                    return (TNode)node.GetSlot(node.SlotCount - 1);
                }
                return (TNode)node;
            }
        }

        public void CopyTo(int offset, ArrayElement<SyntaxNode>[] array, int arrayOffset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                array[arrayOffset + i].Value = this[i + offset];
            }
        }
        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node == right._node;
        }
        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return left._node != right._node;
        }

        public bool Equals(SyntaxList<TNode> other)
        {
            return _node == other._node;
        }

        public override bool Equals(object obj)
        {
            return (obj is SyntaxList<TNode>) && Equals((SyntaxList<TNode>)obj);
        }

        public override int GetHashCode()
        {
            return _node != null ? _node.GetHashCode() : 0;
        }

        public SeparatedSyntaxList<TOther> AsSeparatedList<TOther>() where TOther : SyntaxNode
        {
            return new SeparatedSyntaxList<TOther>(this);
        }


        public static implicit operator SyntaxList<TNode>(TNode node)
        {
            return new SyntaxList<TNode>(node);
        }
        public static implicit operator SyntaxList<TNode>(SyntaxList<SyntaxNode> nodes)
        {
            return new SyntaxList<TNode>(nodes._node);
        }
        public static implicit operator SyntaxList<SyntaxNode>(SyntaxList<TNode> nodes)
        {
            return new SyntaxList<SyntaxNode>(nodes.Node);
        }
    }
}
