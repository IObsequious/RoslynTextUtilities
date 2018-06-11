using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class CompositeText : SourceText
    {
        private readonly int _length;
        private readonly int _storageSize;
        private readonly int[] _segmentOffsets;

        private CompositeText(ImmutableArray<SourceText> segments, Encoding encoding, SourceHashAlgorithm checksumAlgorithm)
            : base(checksumAlgorithm: checksumAlgorithm)
        {
            Debug.Assert(!segments.IsDefaultOrEmpty);
            Segments = segments;
            Encoding = encoding;
            ComputeLengthAndStorageSize(segments, out _length, out _storageSize);
            _segmentOffsets = new int[segments.Length];
            int offset = 0;
            for (int i = 0; i < _segmentOffsets.Length; i++)
            {
                _segmentOffsets[i] = offset;
                offset += Segments[i].Length;
            }
        }

        public override Encoding Encoding { get; }

        public override int Length
        {
            get
            {
                return _length;
            }
        }

        internal override int StorageSize
        {
            get
            {
                return _storageSize;
            }
        }

        internal override ImmutableArray<SourceText> Segments { get; }

        public override char this[int position]
        {
            get
            {
                GetIndexAndOffset(position, out int index, out int offset);
                return Segments[index][offset];
            }
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);
            int sourceIndex = span.Start;
            int count = span.Length;
            GetIndexAndOffset(sourceIndex, out int segIndex, out int segOffset);
            ArrayBuilder<SourceText> newSegments = ArrayBuilder<SourceText>.GetInstance();
            while (segIndex < Segments.Length && count > 0)
            {
                SourceText segment = Segments[segIndex];
                int copyLength = Math.Min(count, segment.Length - segOffset);
                AddSegments(newSegments, segment.GetSubText(new TextSpan(segOffset, copyLength)));
                count -= copyLength;
                segIndex++;
                segOffset = 0;
            }

            return ToSourceTextAndFree(newSegments, this, false);
        }

        private void GetIndexAndOffset(int position, out int index, out int offset)
        {
            int idx = _segmentOffsets.BinarySearch(position);
            index = idx >= 0 ? idx : ~idx - 1;
            offset = position - _segmentOffsets[index];
        }

        private bool CheckCopyToArguments(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }

            if (count < 0 || count > Length - sourceIndex || count > destination.Length - destinationIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return count > 0;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!CheckCopyToArguments(sourceIndex, destination, destinationIndex, count))
            {
                return;
            }

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);
            while (segIndex < Segments.Length && count > 0)
            {
                SourceText segment = Segments[segIndex];
                int copyLength = Math.Min(count, segment.Length - segOffset);
                segment.CopyTo(segOffset, destination, destinationIndex, copyLength);
                count -= copyLength;
                destinationIndex += copyLength;
                segIndex++;
                segOffset = 0;
            }
        }

        public static void AddSegments(ArrayBuilder<SourceText> segments, SourceText text)
        {
            CompositeText composite = text as CompositeText;
            if (composite == null)
            {
                segments.Add(text);
            }
            else
            {
                segments.AddRange(composite.Segments);
            }
        }

        public static SourceText ToSourceTextAndFree(ArrayBuilder<SourceText> segments, SourceText original, bool adjustSegments)
        {
            if (adjustSegments)
            {
                TrimInaccessibleText(segments);
                ReduceSegmentCountIfNecessary(segments);
            }

            if (segments.Count == 0)
            {
                segments.Free();
                return From(string.Empty, original.Encoding, original.ChecksumAlgorithm);
            }

            if (segments.Count == 1)
            {
                SourceText result = segments[0];
                segments.Free();
                return result;
            }

            return new CompositeText(segments.ToImmutableAndFree(), original.Encoding, original.ChecksumAlgorithm);
        }

        internal const int TARGET_SEGMENT_COUNT_AFTER_REDUCTION = 32;
        internal const int MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION = 64;

        private static void ReduceSegmentCountIfNecessary(ArrayBuilder<SourceText> segments)
        {
            if (segments.Count > MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION)
            {
                int segmentSize = GetMinimalSegmentSizeToUseForCombining(segments);
                CombineSegments(segments, segmentSize);
            }
        }

        private const int INITIAL_SEGMENT_SIZE_FOR_COMBINING = 32;
        private const int MAXIMUM_SEGMENT_SIZE_FOR_COMBINING = int.MaxValue / 16;

        private static int GetMinimalSegmentSizeToUseForCombining(ArrayBuilder<SourceText> segments)
        {
            for (int segmentSize = INITIAL_SEGMENT_SIZE_FOR_COMBINING;
                segmentSize <= MAXIMUM_SEGMENT_SIZE_FOR_COMBINING;
                segmentSize *= 2)
            {
                if (GetSegmentCountIfCombined(segments, segmentSize) <= TARGET_SEGMENT_COUNT_AFTER_REDUCTION)
                {
                    return segmentSize;
                }
            }

            return MAXIMUM_SEGMENT_SIZE_FOR_COMBINING;
        }

        private static int GetSegmentCountIfCombined(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            int numberOfSegmentsReduced = 0;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length <= segmentSize)
                        {
                            count++;
                        }
                    }

                    if (count > 1)
                    {
                        int removed = count - 1;
                        numberOfSegmentsReduced += removed;
                        i += removed;
                    }
                }
            }

            return segments.Count - numberOfSegmentsReduced;
        }

        private static void CombineSegments(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    int combinedLength = segments[i].Length;
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length <= segmentSize)
                        {
                            count++;
                            combinedLength += segments[j].Length;
                        }
                    }

                    if (count > 1)
                    {
                        Encoding encoding = segments[i].Encoding;
                        SourceHashAlgorithm algorithm = segments[i].ChecksumAlgorithm;
                        SourceTextWriter writer = SourceTextWriter.Create(encoding, algorithm, combinedLength);
                        while (count > 0)
                        {
                            segments[i].Write(writer);
                            segments.RemoveAt(i);
                            count--;
                        }

                        SourceText newText = writer.ToSourceText();
                        segments.Insert(i, newText);
                    }
                }
            }
        }

        private static readonly ObjectPool<HashSet<SourceText>> s_uniqueSourcesPool
            = new ObjectPool<HashSet<SourceText>>(factory: () => new HashSet<SourceText>(), size: 5);

        private static void ComputeLengthAndStorageSize(IReadOnlyList<SourceText> segments, out int length, out int size)
        {
            HashSet<SourceText> uniqueSources = s_uniqueSourcesPool.Allocate();
            length = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                SourceText segment = segments[i];
                length += segment.Length;
                uniqueSources.Add(segment.StorageKey);
            }

            size = 0;
            foreach (SourceText segment in uniqueSources)
            {
                size += segment.StorageSize;
            }

            uniqueSources.Clear();
            s_uniqueSourcesPool.Free(uniqueSources);
        }

        private static void TrimInaccessibleText(ArrayBuilder<SourceText> segments)
        {
            int length, size;
            ComputeLengthAndStorageSize(segments, out length, out size);
            if (length < size / 2)
            {
                Encoding encoding = segments[0].Encoding;
                SourceHashAlgorithm algorithm = segments[0].ChecksumAlgorithm;
                SourceTextWriter writer = SourceTextWriter.Create(encoding, algorithm, length);
                foreach (SourceText segment in segments)
                {
                    segment.Write(writer);
                }

                segments.Clear();
                segments.Add(writer.ToSourceText());
            }
        }
    }
}
