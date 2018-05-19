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
        private readonly string _source;
        private readonly Encoding _encodingOpt;

        internal StringText(
            string source,
            Encoding encodingOpt,
            ImmutableArray<byte> checksum = default(ImmutableArray<byte>),
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            ImmutableArray<byte> embeddedTextBlob = default(ImmutableArray<byte>))
            : base(checksum, checksumAlgorithm, embeddedTextBlob)
        {
            Debug.Assert(source != null);
            _source = source;
            _encodingOpt = encodingOpt;
        }

        public override Encoding Encoding
        {
            get
            {
                return _encodingOpt;
            }
        }

        public string Source
        {
            get
            {
                return _source;
            }
        }

        public override int Length
        {
            get
            {
                return _source.Length;
            }
        }

        public override char this[int position]
        {
            get
            {
                return _source[position];
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

        public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (span.Start == 0 && span.End == Length)
            {
                textWriter.Write(Source);
            }
            else
            {
                base.Write(textWriter, span, cancellationToken);
            }
        }
    }
}
