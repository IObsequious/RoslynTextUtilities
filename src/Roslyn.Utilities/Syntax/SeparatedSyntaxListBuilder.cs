
using System;

namespace Microsoft.CodeAnalysis
{

    public struct SeparatedSyntaxListBuilder<TNode> where TNode : SyntaxNode
    {
        private readonly SyntaxListBuilder _builder;

        public SeparatedSyntaxListBuilder(int size)
                    : this(new SyntaxListBuilder(size))
        {
        }

        public static SeparatedSyntaxListBuilder<TNode> Create()
        {
            return new SeparatedSyntaxListBuilder<TNode>(8);
        }

        public SeparatedSyntaxListBuilder(SyntaxListBuilder builder)
        {
            _builder = builder;
        }

        public bool IsNull
        {
            get
            {
                return _builder == null;
            }
        }

        public int Count
        {
            get
            {
                return _builder.Count;
            }
        }
        public SyntaxNode this[int index]
        {
            get
            {
                return _builder[index];
            }
            set
            {
                _builder[index] = value;
            }
        }

        public void Clear()
        {
            _builder.Clear();
        }

        public void RemoveLast()
        {
            _builder.RemoveLast();
        }

        public SeparatedSyntaxListBuilder<TNode> Add(TNode node)
        {
            _builder.Add(node);
            return this;
        }

        public void AddSeparator(SyntaxNode separatorToken)
        {
            _builder.Add(separatorToken);
        }

        public void AddRange(TNode[] items, int offset, int length)
        {
            _builder.AddRange(items, offset, length);
        }

        public void AddRange(SeparatedSyntaxList<TNode> nodes)
        {
            _builder.AddRange(nodes.GetWithSeparators());
        }

        public void AddRange(SeparatedSyntaxList<TNode> nodes, int count)
        {
            var list = nodes.GetWithSeparators();
            _builder.AddRange(list, Count, Math.Min(count * 2, list.Count));
        }

        public bool Any(int kind)
        {
            return _builder.Any(kind);
        }

        public SeparatedSyntaxList<TNode> ToList()
        {
            return _builder == null
                ? default(SeparatedSyntaxList<TNode>)
                : new SeparatedSyntaxList<TNode>(new SyntaxList<SyntaxNode>(_builder.ToListNode()));
        }

        public SyntaxListBuilder UnderlyingBuilder
        {
            get
            {
                return _builder;
            }
        }
        public static implicit operator SeparatedSyntaxList<TNode>(SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder.ToList();
        }
        public static implicit operator SyntaxListBuilder(SeparatedSyntaxListBuilder<TNode> builder)
        {
            return builder._builder;
        }
    }
}
