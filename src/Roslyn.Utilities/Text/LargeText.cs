using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class LargeText : SourceText
    {
        internal const int ChunkSize = LargeObjectHeapLimitInChars;
        private readonly ImmutableArray<char[]> _chunks;
        private readonly int[] _chunkStartOffsets;

        internal LargeText(ImmutableArray<char[]> chunks,
            Encoding encodingOpt,
            ImmutableArray<byte> checksum,
            SourceHashAlgorithm checksumAlgorithm,
            ImmutableArray<byte> embeddedTextBlob)
            : base(checksum, checksumAlgorithm, embeddedTextBlob)
        {
            _chunks = chunks;
            Encoding = encodingOpt;
            _chunkStartOffsets = new int[chunks.Length];
            int offset = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                _chunkStartOffsets[i] = offset;
                offset += chunks[i].Length;
            }

            Length = offset;
        }

        internal LargeText(ImmutableArray<char[]> chunks, Encoding encodingOpt, SourceHashAlgorithm checksumAlgorithm)
            : this(chunks, encodingOpt, default, checksumAlgorithm, default)
        {
        }

        public static SourceText Decode(Stream stream,
            Encoding encoding,
            SourceHashAlgorithm checksumAlgorithm,
            bool throwIfBinaryDetected)
        {
            stream.Seek(0, SeekOrigin.Begin);
            long longLength = stream.Length;
            if (longLength == 0)
            {
                return From(string.Empty, encoding, checksumAlgorithm);
            }

            int maxCharRemainingGuess = encoding.GetMaxCharCountOrThrowIfHuge(stream);
            Debug.Assert(longLength > 0 && longLength <= int.MaxValue);
            int length = (int) longLength;
            using (StreamReader reader = new StreamReader(stream, encoding, true, Math.Min(length, 4096), true))
            {
                ImmutableArray<char[]> chunks = ReadChunksFromTextReader(reader, maxCharRemainingGuess, throwIfBinaryDetected);
                ImmutableArray<byte> checksum = CalculateChecksum(stream, checksumAlgorithm);
                ImmutableArray<byte> embeddedTextBlob = default(ImmutableArray<byte>);
                return new LargeText(chunks, reader.CurrentEncoding, checksum, checksumAlgorithm, embeddedTextBlob);
            }
        }

        public static SourceText Decode(TextReader reader, int length, Encoding encodingOpt, SourceHashAlgorithm checksumAlgorithm)
        {
            if (length == 0)
            {
                return From(string.Empty, encodingOpt, checksumAlgorithm);
            }

            ImmutableArray<char[]> chunks = ReadChunksFromTextReader(reader, length, false);
            return new LargeText(chunks, encodingOpt, checksumAlgorithm);
        }

        private static ImmutableArray<char[]> ReadChunksFromTextReader(TextReader reader,
            int maxCharRemainingGuess,
            bool throwIfBinaryDetected)
        {
            ArrayBuilder<char[]> chunks = ArrayBuilder<char[]>.GetInstance(1 + (maxCharRemainingGuess / ChunkSize));
            while (reader.Peek() != -1)
            {
                int nextChunkSize = ChunkSize;
                if (maxCharRemainingGuess < ChunkSize)
                {
                    nextChunkSize = Math.Max(maxCharRemainingGuess - 64, 64);
                }

                char[] chunk = new char[nextChunkSize];
                int charsRead = reader.ReadBlock(chunk, 0, chunk.Length);
                if (charsRead == 0)
                {
                    break;
                }

                maxCharRemainingGuess -= charsRead;
                if (charsRead < chunk.Length)
                {
                    Array.Resize(ref chunk, charsRead);
                }

                if (throwIfBinaryDetected && IsBinary(chunk))
                {
                    throw new InvalidDataException();
                }

                chunks.Add(chunk);
            }

            return chunks.ToImmutableAndFree();
        }

        private static bool IsBinary(char[] chunk)
        {
            for (int i = 1; i < chunk.Length;)
            {
                if (chunk[i] == '\0')
                {
                    if (chunk[i - 1] == '\0')
                    {
                        return true;
                    }

                    i++;
                }
                else
                {
                    i += 2;
                }
            }

            return false;
        }

        private int GetIndexFromPosition(int position)
        {
            int idx = _chunkStartOffsets.BinarySearch(position);
            return idx >= 0 ? idx : ~idx - 1;
        }

        public override char this[int position]
        {
            get
            {
                int i = GetIndexFromPosition(position);
                return _chunks[i][position - _chunkStartOffsets[i]];
            }
        }

        public override Encoding Encoding { get; }

        public override int Length { get; }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (count == 0)
            {
                return;
            }

            int chunkIndex = GetIndexFromPosition(sourceIndex);
            int chunkStartOffset = sourceIndex - _chunkStartOffsets[chunkIndex];
            while (true)
            {
                char[] chunk = _chunks[chunkIndex];
                int charsToCopy = Math.Min(chunk.Length - chunkStartOffset, count);
                Array.Copy(chunk, chunkStartOffset, destination, destinationIndex, charsToCopy);
                count -= charsToCopy;
                if (count <= 0)
                {
                    break;
                }

                destinationIndex += charsToCopy;
                chunkStartOffset = 0;
                chunkIndex++;
            }
        }

        public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default)
        {
            if (span.Start < 0 || span.Start > Length || span.End > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            int count = span.Length;
            if (count == 0)
            {
                return;
            }

            LargeTextWriter chunkWriter = writer as LargeTextWriter;
            int chunkIndex = GetIndexFromPosition(span.Start);
            int chunkStartOffset = span.Start - _chunkStartOffsets[chunkIndex];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                char[] chunk = _chunks[chunkIndex];
                int charsToWrite = Math.Min(chunk.Length - chunkStartOffset, count);
                if (chunkWriter != null && chunkStartOffset == 0 && charsToWrite == chunk.Length)
                {
                    chunkWriter.AppendChunk(chunk);
                }
                else
                {
                    writer.Write(chunk, chunkStartOffset, charsToWrite);
                }

                count -= charsToWrite;
                if (count <= 0)
                {
                    break;
                }

                chunkStartOffset = 0;
                chunkIndex++;
            }
        }

        protected override TextLineCollection GetLinesCore()
        {
            return new LineInfo(this, ParseLineStarts());
        }

        private int[] ParseLineStarts()
        {
            int position = 0;
            int index = 0;
            int lastCr = -1;
            ArrayBuilder<int> arrayBuilder = ArrayBuilder<int>.GetInstance();
            foreach (char[] chunk in _chunks)
            {
                foreach (char c in chunk)
                {
                    index++;
                    const uint bias = '\r' + 1;
                    if (unchecked(c - bias) <= 127 - bias)
                    {
                        continue;
                    }

                    switch (c)
                    {
                        case '\r':
                            lastCr = index;
                            goto line_break;
                        case '\n':
                            if (lastCr == index - 1)
                            {
                                position = index;
                                break;
                            }

                            goto line_break;
                        case '\u0085':
                        case '\u2028':
                        case '\u2029':
                            line_break:
                            arrayBuilder.Add(position);
                            position = index;
                            break;
                    }
                }
            }

            arrayBuilder.Add(position);
            return arrayBuilder.ToArrayAndFree();
        }
    }
}
