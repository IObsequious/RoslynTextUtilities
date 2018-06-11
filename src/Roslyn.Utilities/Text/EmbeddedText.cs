using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class EmbeddedText
    {
        internal const int CompressionThreshold = 200;

        public string FilePath { get; }

        public SourceHashAlgorithm ChecksumAlgorithm { get; }

        public ImmutableArray<byte> Checksum { get; }

        private EmbeddedText(string filePath,
            ImmutableArray<byte> checksum,
            SourceHashAlgorithm checksumAlgorithm,
            ImmutableArray<byte> blob)
        {
            Debug.Assert(filePath?.Length > 0);
            Debug.Assert(!blob.IsDefault && blob.Length >= sizeof(int));
            FilePath = filePath;
            Checksum = checksum;
            ChecksumAlgorithm = checksumAlgorithm;
            Blob = blob;
        }

        internal ImmutableArray<byte> Blob { get; }

        private sealed class CountingDeflateStream : DeflateStream
        {
            public CountingDeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
                : base(stream, compressionLevel, leaveOpen)
            {
            }

            public int BytesWritten { get; private set; }

            public override void Write(byte[] array, int offset, int count)
            {
                base.Write(array, offset, count);
                checked
                {
                    BytesWritten += count;
                }
            }

            public override void WriteByte(byte value)
            {
                base.WriteByte(value);
                checked
                {
                    BytesWritten++;
                }
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
