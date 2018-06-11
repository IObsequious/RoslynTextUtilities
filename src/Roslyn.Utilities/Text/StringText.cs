using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class StringText : SourceText
    {
        internal StringText(
            string source,
            Encoding encodingOpt,
            ImmutableArray<byte> checksum = default,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            ImmutableArray<byte> embeddedTextBlob = default)
            : base(checksum, checksumAlgorithm, embeddedTextBlob)
        {
            Debug.Assert(source != null);
            Source = source;
            Encoding = encodingOpt;
        }

        public override Encoding Encoding { get; }

        public string Source { get; }

        public override int Length
        {
            get
            {
                return Source.Length;
            }
        }

        public override char this[int position]
        {
            get
            {
                return Source[position];
            }
        }

        public override string ToString(TextSpan span)
        {
            if (span.End > Source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (span.Start == 0 && span.Length == Length)
            {
                return Source;
            }

            return Source.Substring(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            Source.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default)
        {
            if (span.Start == 0 && span.End == Length)
            {
                writer.Write(Source);
            }
            else
            {
                base.Write(writer, span, cancellationToken);
            }
        }
    }
}
