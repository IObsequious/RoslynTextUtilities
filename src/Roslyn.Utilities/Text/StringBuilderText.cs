using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed partial class StringBuilderText : SourceText
    {
        public StringBuilderText(StringBuilder builder, Encoding encodingOpt, SourceHashAlgorithm checksumAlgorithm)
            : base(checksumAlgorithm: checksumAlgorithm)
        {
            Debug.Assert(builder != null);
            Builder = builder;
            Encoding = encodingOpt;
        }

        public override Encoding Encoding { get; }

        internal StringBuilder Builder { get; }

        public override int Length
        {
            get
            {
                return Builder.Length;
            }
        }

        public override char this[int position]
        {
            get
            {
                if (position < 0 || position >= Builder.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                return Builder[position];
            }
        }

        public override string ToString(TextSpan span)
        {
            if (span.End > Builder.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            return Builder.ToString(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            Builder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
