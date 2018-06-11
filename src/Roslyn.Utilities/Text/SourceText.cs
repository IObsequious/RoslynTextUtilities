using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public abstract class SourceText
    {
        private const int CharBufferSize = 32 * 1024;
        private const int CharBufferCount = 5;
        public const int LargeObjectHeapLimitInChars = 40 * 1024;

        private static readonly ObjectPool<char[]>
            s_charArrayPool = new ObjectPool<char[]>(() => new char[CharBufferSize], CharBufferCount);

        private SourceTextContainer _lazyContainer;
        private TextLineCollection _lazyLineInfo;
        private ImmutableArray<byte> _lazyChecksum;

        protected SourceText(ImmutableArray<byte> checksum = default,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            SourceTextContainer container = null)
        {
            ValidateChecksumAlgorithm(checksumAlgorithm);
            if (!checksum.IsDefault && checksum.Length != CryptographicHashProvider.GetHashSize(checksumAlgorithm))
            {
                throw new ArgumentException(nameof(checksum));
            }

            ChecksumAlgorithm = checksumAlgorithm;
            _lazyChecksum = checksum;
            _lazyContainer = container;
        }

        internal SourceText(ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> embeddedTextBlob)
            : this(checksum, checksumAlgorithm)
        {
            Debug.Assert(embeddedTextBlob.IsDefault || !checksum.IsDefault);
            if (!checksum.IsDefault && embeddedTextBlob.IsDefault)
            {
                PrecomputedEmbeddedTextBlob = ImmutableArray<byte>.Empty;
            }
            else
            {
                PrecomputedEmbeddedTextBlob = embeddedTextBlob;
            }
        }

        internal static void ValidateChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        {
            if (!Enum.IsDefined(typeof(SourceHashAlgorithm), checksumAlgorithm))
                throw new InvalidEnumArgumentException(nameof(checksumAlgorithm), (int) checksumAlgorithm, typeof(SourceHashAlgorithm));
        }

        public static SourceText From(string text,
    Encoding encoding = null,
    SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new StringText(text, encoding, checksumAlgorithm: checksumAlgorithm);
        }

        public static SourceText From(
            TextReader reader,
            Encoding encoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            return From(reader.ReadToEnd(), encoding ?? Encoding.GetEncoding("utf-16"), checksumAlgorithm);
        }

        public static SourceText From(
    TextReader reader,
    int length,
    Encoding encoding = null,
    SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (length >= LargeObjectHeapLimitInChars)
            {
                return LargeText.Decode(reader, length, encoding, checksumAlgorithm);
            }

            string text = reader.ReadToEnd();
            return From(text, encoding, checksumAlgorithm);
        }

        internal static bool IsBinary(string text)
        {
            for (int i = 1; i < text.Length;)
            {
                if (text[i] == '\0')
                {
                    if (text[i - 1] == '\0')
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

        public SourceHashAlgorithm ChecksumAlgorithm { get; }

        public abstract Encoding Encoding { get; }

        public abstract int Length { get; }

        internal virtual int StorageSize
        {
            get
            {
                return Length;
            }
        }

        internal virtual ImmutableArray<SourceText> Segments
        {
            get
            {
                return ImmutableArray<SourceText>.Empty;
            }
        }

        internal virtual SourceText StorageKey
        {
            get
            {
                return this;
            }
        }

        public bool CanBeEmbedded
        {
            get
            {
                if (PrecomputedEmbeddedTextBlob.IsDefault)
                {
                    return Encoding != null;
                }

                return !PrecomputedEmbeddedTextBlob.IsEmpty;
            }
        }

        internal ImmutableArray<byte> PrecomputedEmbeddedTextBlob { get; }

        public abstract char this[int position] { get; }

        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        public virtual SourceTextContainer Container
        {
            get
            {
                if (_lazyContainer == null)
                {
                    Interlocked.CompareExchange(ref _lazyContainer, new StaticContainer(this), null);
                }

                return _lazyContainer;
            }
        }

        internal void CheckSubSpan(TextSpan span)
        {
            if (span.Start < 0 || span.Start > Length || span.End > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }
        }

        public virtual SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);
            int spanLength = span.Length;
            if (spanLength == 0)
            {
                return From(string.Empty, Encoding, ChecksumAlgorithm);
            }

            if (spanLength == Length && span.Start == 0)
            {
                return this;
            }

            return new SubText(this, span);
        }

        public SourceText GetSubText(int start)
        {
            if (start < 0 || start > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start == 0)
            {
                return this;
            }

            return GetSubText(new TextSpan(start, Length - start));
        }

        public void Write(
            TextWriter textWriter,
            CancellationToken cancellationToken = default)
        {
            Write(textWriter, new TextSpan(0, Length), cancellationToken);
        }

        public virtual void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default)
        {
            CheckSubSpan(span);
            char[] buffer = s_charArrayPool.Allocate();
            try
            {
                int offset = Math.Min(Length, span.Start);
                int length = Math.Min(Length, span.End) - offset;
                while (offset < length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int count = Math.Min(buffer.Length, length - offset);
                    CopyTo(offset, buffer, 0, count);
                    writer.Write(buffer, 0, count);
                    offset += count;
                }
            }
            finally
            {
                s_charArrayPool.Free(buffer);
            }
        }

        public ImmutableArray<byte> GetChecksum()
        {
            if (_lazyChecksum.IsDefault)
            {
                using (SourceTextStream stream = new SourceTextStream(this, useDefaultEncodingIfNull: true))
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChecksum, CalculateChecksum(stream, ChecksumAlgorithm));
                }
            }

            return _lazyChecksum;
        }

        internal static ImmutableArray<byte> CalculateChecksum(byte[] buffer, int offset, int count, SourceHashAlgorithm algorithmId)
        {
            using (HashAlgorithm algorithm = CryptographicHashProvider.TryGetAlgorithm(algorithmId))
            {
                Debug.Assert(algorithm != null);
                return ImmutableArray.Create(algorithm.ComputeHash(buffer, offset, count));
            }
        }

        internal static ImmutableArray<byte> CalculateChecksum(Stream stream, SourceHashAlgorithm algorithmId)
        {
            using (HashAlgorithm algorithm = CryptographicHashProvider.TryGetAlgorithm(algorithmId))
            {
                Debug.Assert(algorithm != null);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                return ImmutableArray.Create(algorithm.ComputeHash(stream));
            }
        }

        public override string ToString()
        {
            return ToString(new TextSpan(0, Length));
        }

        public char[] ToCharArray() => ToString().ToCharArray();

        public virtual string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            StringBuilder builder = new StringBuilder();
            char[] buffer = new char[Math.Min(span.Length, 1024)];
            int position = Math.Max(Math.Min(span.Start, Length), 0);
            int length = Math.Min(span.End, Length) - position;
            while (position < Length && length > 0)
            {
                int copyLength = Math.Min(buffer.Length, length);
                CopyTo(position, buffer, 0, copyLength);
                builder.Append(buffer, 0, copyLength);
                length -= copyLength;
                position += copyLength;
            }

            return builder.ToString();
        }

        #region Changes

        public virtual SourceText WithChanges(IEnumerable<TextChange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (!changes.Any())
            {
                return this;
            }

            ArrayBuilder<SourceText> segments = ArrayBuilder<SourceText>.GetInstance();
            ArrayBuilder<TextChangeRange> changeRanges = ArrayBuilder<TextChangeRange>.GetInstance();
            int position = 0;
            foreach (TextChange change in changes)
            {
                if (change.Span.Start < position)
                {
                    throw new ArgumentException(nameof(changes));
                }

                int newTextLength = change.NewText?.Length ?? 0;

                if (change.Span.Length == 0 && newTextLength == 0)
                {
                    continue;
                }

                if (change.Span.Start > position)
                {
                    SourceText subText = GetSubText(new TextSpan(position, change.Span.Start - position));
                    CompositeText.AddSegments(segments, subText);
                }

                if (newTextLength > 0)
                {
                    SourceText segment = From(change.NewText, Encoding, ChecksumAlgorithm);
                    CompositeText.AddSegments(segments, segment);
                }

                position = change.Span.End;
                changeRanges.Add(new TextChangeRange(change.Span, newTextLength));
            }

            if (position == 0 && segments.Count == 0)
            {
                changeRanges.Free();
                return this;
            }

            if (position < Length)
            {
                SourceText subText = GetSubText(new TextSpan(position, Length - position));
                CompositeText.AddSegments(segments, subText);
            }

            SourceText newText = CompositeText.ToSourceTextAndFree(segments, this, true);
            if (newText != this)
            {
                return new ChangedText(this, newText, changeRanges.ToImmutableAndFree());
            }

            return this;
        }

        public SourceText WithChanges(params TextChange[] changes)
        {
            return WithChanges((IEnumerable<TextChange>)changes);
        }

        public SourceText Replace(TextSpan span, string newText)
        {
            return WithChanges(new TextChange(span, newText));
        }

        public SourceText Replace(int start, int length, string newText)
        {
            return Replace(new TextSpan(start, length), newText);
        }

        public virtual IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (oldText == this)
            {
                return TextChangeRange.NoChanges;
            }

            return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), Length));
        }

        public virtual IReadOnlyList<TextChange> GetTextChanges(SourceText oldText)
        {
            int newPosDelta = 0;
            List<TextChangeRange> ranges = GetChangeRanges(oldText).ToList();
            List<TextChange> textChanges = new List<TextChange>(ranges.Count);
            foreach (TextChangeRange range in ranges)
            {
                int newPos = range.Span.Start + newPosDelta;

                string newt;
                if (range.NewLength > 0)
                {
                    TextSpan span = new TextSpan(newPos, range.NewLength);
                    newt = ToString(span);
                }
                else
                {
                    newt = string.Empty;
                }

                textChanges.Add(new TextChange(range.Span, newt));
                newPosDelta += range.NewLength - range.Span.Length;
            }

            return textChanges.ToImmutableArray();
        }

        #endregion

        #region Lines

        public TextLineCollection Lines
        {
            get
            {
                TextLineCollection info = _lazyLineInfo;
                return info ?? Interlocked.CompareExchange(ref _lazyLineInfo, info = GetLinesCore(), null) ?? info;
            }
        }

        internal bool TryGetLines(out TextLineCollection lines)
        {
            lines = _lazyLineInfo;
            return lines != null;
        }

        protected virtual TextLineCollection GetLinesCore()
        {
            return new LineInfo(this, ParseLineStarts());
        }

        internal sealed class LineInfo : TextLineCollection
        {
            private readonly SourceText _text;
            private readonly int[] _lineStarts;
            private int _lastLineNumber;

            public LineInfo(SourceText text, int[] lineStarts)
            {
                _text = text;
                _lineStarts = lineStarts;
            }

            public override int Count => _lineStarts.Length;

            public override TextLine this[int index]
            {
                get
                {
                    if (index < 0 || index >= _lineStarts.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    int start = _lineStarts[index];
                    if (index == _lineStarts.Length - 1)
                    {
                        return TextLine.FromSpan(_text, TextSpan.FromBounds(start, _text.Length));
                    }

                    int end = _lineStarts[index + 1];
                    return TextLine.FromSpan(_text, TextSpan.FromBounds(start, end));
                }
            }

            public override int IndexOf(int position)
            {
                if (position < 0 || position > _text.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                int lineNumber;

                int lastLineNumber = _lastLineNumber;
                if (position >= _lineStarts[lastLineNumber])
                {
                    int limit = Math.Min(_lineStarts.Length, lastLineNumber + 4);
                    for (int i = lastLineNumber; i < limit; i++)
                    {
                        if (position < _lineStarts[i])
                        {
                            lineNumber = i - 1;
                            _lastLineNumber = lineNumber;
                            return lineNumber;
                        }
                    }
                }

                lineNumber = _lineStarts.BinarySearch(position);
                if (lineNumber < 0)
                {
                    lineNumber = ~lineNumber - 1;
                }

                _lastLineNumber = lineNumber;
                return lineNumber;
            }

            public override TextLine GetLineFromPosition(int position)
            {
                return this[IndexOf(position)];
            }
        }

        private void EnumerateChars(Action<int, char[], int> action)
        {
            int position = 0;
            char[] buffer = s_charArrayPool.Allocate();
            int length = Length;
            while (position < length)
            {
                int contentLength = Math.Min(length - position, buffer.Length);
                CopyTo(position, buffer, 0, contentLength);
                action(position, buffer, contentLength);
                position += contentLength;
            }

            action(position, buffer, 0);
            s_charArrayPool.Free(buffer);
        }

        private int[] ParseLineStarts()
        {
            if (0 == Length)
            {
                return new[] { 0 };
            }

            ArrayBuilder<int> lineStarts = ArrayBuilder<int>.GetInstance();
            lineStarts.Add(0);
            bool lastWasCR = false;

            EnumerateChars((position, buffer, length) =>
{
int index = 0;
if (lastWasCR)
{
if (length > 0 && buffer[0] == '\n')
{
index++;
}

lineStarts.Add(position + index);
lastWasCR = false;
}

while (index < length)
{
char c = buffer[index];
index++;

const uint bias = '\r' + 1;
if (unchecked(c - bias) <= 127 - bias)
{
continue;
}

if (c == '\r')
{
if (index < length && buffer[index] == '\n')
{
index++;
}
else if (index >= length)
{
lastWasCR = true;
continue;
}
}
else if (!TextUtilities.IsAnyLineBreakCharacter(c))
{
continue;
}

lineStarts.Add(position + index);
}
});
            return lineStarts.ToArrayAndFree();
        }

        #endregion

        public bool ContentEquals(SourceText other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            ImmutableArray<byte> leftChecksum = _lazyChecksum;
            ImmutableArray<byte> rightChecksum = other._lazyChecksum;
            if (!leftChecksum.IsDefault
                && !rightChecksum.IsDefault
                && Encoding == other.Encoding
                && ChecksumAlgorithm == other.ChecksumAlgorithm)
            {
                return leftChecksum.SequenceEqual(rightChecksum);
            }

            return ContentEqualsImpl(other);
        }

        protected virtual bool ContentEqualsImpl(SourceText other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Length != other.Length)
            {
                return false;
            }

            char[] buffer1 = s_charArrayPool.Allocate();
            char[] buffer2 = s_charArrayPool.Allocate();
            try
            {
                int position = 0;
                while (position < Length)
                {
                    int n = Math.Min(Length - position, buffer1.Length);
                    CopyTo(position, buffer1, 0, n);
                    other.CopyTo(position, buffer2, 0, n);
                    for (int i = 0; i < n; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            return false;
                        }
                    }

                    position += n;
                }

                return true;
            }
            finally
            {
                s_charArrayPool.Free(buffer2);
                s_charArrayPool.Free(buffer1);
            }
        }

        internal static Encoding TryReadByteOrderMark(byte[] source, int length, out int preambleLength)
        {
            Debug.Assert(source != null);
            Debug.Assert(length <= source.Length);
            if (length >= 2)
            {
                switch (source[0])
                {
                    case 0xFE:
                        if (source[1] == 0xFF)
                        {
                            preambleLength = 2;
                            return Encoding.BigEndianUnicode;
                        }

                        break;
                    case 0xFF:
                        if (source[1] == 0xFE)
                        {
                            preambleLength = 2;
                            return Encoding.Unicode;
                        }

                        break;
                    case 0xEF:
                        if (source[1] == 0xBB && length >= 3 && source[2] == 0xBF)
                        {
                            preambleLength = 3;
                            return Encoding.UTF8;
                        }

                        break;
                }
            }

            preambleLength = 0;
            return null;
        }

        private class StaticContainer : SourceTextContainer
        {
            public StaticContainer(SourceText text)
            {
                CurrentText = text;
            }

            public override SourceText CurrentText { get; }

            public override event EventHandler<TextChangeEventArgs> TextChanged
            {
                add
                {
                }
                remove
                {
                }
            }
        }
    }
}
