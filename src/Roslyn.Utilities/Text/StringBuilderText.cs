using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed partial class StringBuilderText : SourceText
    {
        private readonly StringBuilder _builder;
        private readonly Encoding _encodingOpt;

        public StringBuilderText(StringBuilder builder, Encoding encodingOpt, SourceHashAlgorithm checksumAlgorithm)
            : base(checksumAlgorithm: checksumAlgorithm)
        {
            Debug.Assert(builder != null);
            _builder = builder;
            _encodingOpt = encodingOpt;
        }

        public override Encoding Encoding
        {
            get
            {
                return _encodingOpt;
            }
        }

        internal StringBuilder Builder
        {
            get
            {
                return _builder;
            }
        }

        public override int Length
        {
            get
            {
                return _builder.Length;
            }
        }

        public override char this[int position]
        {
            get
            {
                if (position < 0 || position >= _builder.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                return _builder[position];
            }
        }

        public override string ToString(TextSpan span)
        {
            if (span.End > _builder.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            return _builder.ToString(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
