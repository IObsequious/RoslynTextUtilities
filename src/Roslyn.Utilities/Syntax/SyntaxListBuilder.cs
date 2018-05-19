
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{

    public class SyntaxListBuilder
    {
        private ArrayElement<SyntaxNode>[] _nodes;

        public int Count
        {
            get; private set;
        }

        public SyntaxListBuilder(int size)
        {
            _nodes = new ArrayElement<SyntaxNode>[size];
        }

        public static SyntaxListBuilder Create()
        {
            return new SyntaxListBuilder(8);
        }

        public void Clear()
        {
            Count = 0;
        }
        public SyntaxNode this[int index]
        {
            get
            {
                return _nodes[index];
            }
            set
            {
                _nodes[index].Value = value;
            }
        }

        public void Add(SyntaxNode item)
        {
            if (item == null)
                return;
            if (item.IsList)
            {
                int slotCount = item.SlotCount;
                EnsureAdditionalCapacity(slotCount);
                for (int i = 0; i < slotCount; i++)
                {
                    Add(item.GetSlot(i));
                }
            }
            else
            {
                EnsureAdditionalCapacity(1);
                _nodes[Count++].Value = item;
            }
        }

        public void AddRange(SyntaxNode[] items)
        {
            AddRange(items, 0, items.Length);
        }

        public void AddRange(SyntaxNode[] items, int offset, int length)
        {
            EnsureAdditionalCapacity(length - offset);
            int oldCount = Count;
            for (int i = offset; i < length; i++)
            {
                Add(items[i]);
            }
            Validate(oldCount, Count);
        }

        [Conditional("DEBUG")]
        private void Validate(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                Debug.Assert(_nodes[i].Value != null);
            }
        }

        public void AddRange(SyntaxList<SyntaxNode> list)
        {
            AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<SyntaxNode> list, int offset, int length)
        {
            EnsureAdditionalCapacity(length - offset);
            int oldCount = Count;
            for (int i = offset; i < length; i++)
            {
                Add(list[i]);
            }
            Validate(oldCount, Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
        {
            AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : SyntaxNode
        {
            AddRange(new SyntaxList<SyntaxNode>(list.Node), offset, length);
        }

        public void RemoveLast()
        {
            Count--;
            _nodes[Count].Value = null;
        }

        private void EnsureAdditionalCapacity(int additionalCount)
        {
            int currentSize = _nodes.Length;
            int requiredSize = Count + additionalCount;
            if (requiredSize <= currentSize)
                return;
            int newSize =
                requiredSize < 8 ? 8 :
                requiredSize >= (int.MaxValue / 2) ? int.MaxValue :
                Math.Max(requiredSize, currentSize * 2);
            Debug.Assert(newSize >= requiredSize);
            Array.Resize(ref _nodes, newSize);
        }

        public bool Any(int kind)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_nodes[i].Value.RawKind == kind)
                {
                    return true;
                }
            }
            return false;
        }

        public SyntaxNode[] ToArray()
        {
            var array = new SyntaxNode[Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = _nodes[i];
            }
            return array;
        }

        public SyntaxNode ToListNode()
        {
            switch (Count)
            {
                case 0:
                    return null;
                case 1:
                    return _nodes[0];
                case 2:
                    return SyntaxList.List(_nodes[0], _nodes[1]);
                case 3:
                    return SyntaxList.List(_nodes[0], _nodes[1], _nodes[2]);
                default:
                    var tmp = new ArrayElement<SyntaxNode>[Count];
                    Array.Copy(_nodes, tmp, Count);
                    return SyntaxList.List(tmp);
            }
        }

        public SyntaxList<SyntaxNode> ToList()
        {
            return new SyntaxList<SyntaxNode>(ToListNode());
        }

        public SyntaxList<TNode> ToList<TNode>() where TNode : SyntaxNode
        {
            return new SyntaxList<TNode>(ToListNode());
        }
    }
}
