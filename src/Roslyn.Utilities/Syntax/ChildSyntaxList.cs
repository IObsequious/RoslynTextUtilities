

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public struct ChildSyntaxList
    {
        public partial struct Reversed
        {
            private readonly SyntaxNode _node;

            public Reversed(SyntaxNode node)
            {
                _node = node;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_node);
            }
#if DEBUG
#pragma warning disable 618
            [Obsolete("For debugging", error: true)]
            private SyntaxNode[] Nodes
            {
                get
                {
                    var result = new List<SyntaxNode>();
                    foreach (var n in this)
                    {
                        result.Add(n);
                    }

                    return result.ToArray();
                }
            }

#pragma warning restore 618
#endif
        }

        public partial struct Reversed
        {

            public struct Enumerator
            {
                private readonly SyntaxNode _node;
                private int _childIndex;
                private SyntaxNode _list;
                private int _listIndex;
                private SyntaxNode _currentChild;

                public Enumerator(SyntaxNode node)
                {
                    if (node != null)
                    {
                        _node = node;
                        _childIndex = node.SlotCount;
                        _listIndex = -1;
                    }
                    else
                    {
                        _node = null;
                        _childIndex = 0;
                        _listIndex = -1;
                    }
                    _list = null;
                    _currentChild = null;
                }

                public bool MoveNext()
                {
                    if (_node != null)
                    {
                        if (_list != null)
                        {
                            if (--_listIndex >= 0)
                            {
                                _currentChild = _list.GetSlot(_listIndex);
                                return true;
                            }
                            _list = null;
                            _listIndex = -1;
                        }
                        while (--_childIndex >= 0)
                        {
                            var child = _node.GetSlot(_childIndex);
                            if (child == null)
                            {
                                continue;
                            }
                            if (child.IsList)
                            {
                                _list = child;
                                _listIndex = _list.SlotCount;
                                if (--_listIndex >= 0)
                                {
                                    _currentChild = _list.GetSlot(_listIndex);
                                    return true;
                                }
                                else
                                {
                                    _list = null;
                                    _listIndex = -1;
                                    continue;
                                }
                            }
                            else
                            {
                                _currentChild = child;
                            }
                            return true;
                        }
                    }
                    _currentChild = null;
                    return false;
                }

                public SyntaxNode Current
                {
                    get
                    {
                        return _currentChild;
                    }
                }
            }
        }

        public struct Enumerator
        {
            private readonly SyntaxNode _node;
            private int _childIndex;
            private SyntaxNode _list;
            private int _listIndex;
            private SyntaxNode _currentChild;

            public Enumerator(SyntaxNode node)
            {
                _node = node;
                _childIndex = -1;
                _listIndex = -1;
                _list = null;
                _currentChild = null;
            }

            public bool MoveNext()
            {
                if (_node != null)
                {
                    if (_list != null)
                    {
                        _listIndex++;
                        if (_listIndex < _list.SlotCount)
                        {
                            _currentChild = _list.GetSlot(_listIndex);
                            return true;
                        }
                        _list = null;
                        _listIndex = -1;
                    }
                    while (true)
                    {
                        _childIndex++;
                        if (_childIndex == _node.SlotCount)
                        {
                            break;
                        }
                        var child = _node.GetSlot(_childIndex);
                        if (child == null)
                        {
                            continue;
                        }
                        if (child.RawKind == SyntaxNode.ListKind)
                        {
                            _list = child;
                            _listIndex++;
                            if (_listIndex < _list.SlotCount)
                            {
                                _currentChild = _list.GetSlot(_listIndex);
                                return true;
                            }
                            else
                            {
                                _list = null;
                                _listIndex = -1;
                                continue;
                            }
                        }
                        else
                        {
                            _currentChild = child;
                        }
                        return true;
                    }
                }
                _currentChild = null;
                return false;
            }

            public SyntaxNode Current
            {
                get
                {
                    return _currentChild;
                }
            }
        }

        private readonly SyntaxNode _node;
        private int _count;

        public ChildSyntaxList(SyntaxNode node)
        {
            _node = node;
            _count = -1;
        }

        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    _count = CountNodes();
                }
                return _count;
            }
        }

        private int CountNodes()
        {
            int n = 0;
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                n++;
            }
            return n;
        }

        private SyntaxNode[] Nodes
        {
            get
            {
                var result = new SyntaxNode[Count];
                var i = 0;
                foreach (var n in this)
                {
                    result[i++] = n;
                }
                return result;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_node);
        }

        public Reversed Reverse()
        {
            return new Reversed(_node);
        }
    }
}
