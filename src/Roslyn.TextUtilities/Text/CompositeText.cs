﻿// -----------------------------------------------------------------------
// <copyright file="CompositeText.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Linq;

namespace System.Text
{
    /// <summary>
    /// A composite of a sequence of <see cref="SourceText"/>s.
    /// </summary>
    internal sealed class CompositeText : SourceText
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
                int index;
                int offset;
                GetIndexAndOffset(position, out index, out offset);
                return Segments[index][offset];
            }
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);
            var sourceIndex = span.Start;
            var count = span.Length;
            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);
            var newSegments = ArrayBuilder<SourceText>.GetInstance();
            while (segIndex < Segments.Length && count > 0)
            {
                var segment = Segments[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);
                AddSegments(newSegments, segment.GetSubText(new TextSpan(segOffset, copyLength)));
                count -= copyLength;
                segIndex++;
                segOffset = 0;
            }

            return ToSourceTextAndFree(newSegments, this, false);
        }

        private void GetIndexAndOffset(int position, out int index, out int offset)
        {
            // Binary search to find the chunk that contains the given position.
            int idx = _segmentOffsets.BinarySearch(position);
            index = idx >= 0 ? idx : ~idx - 1;
            offset = position - _segmentOffsets[index];
        }

        /// <summary>
        /// Validates the arguments passed to <see cref="CopyTo"/> against the published contract.
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="destination"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        /// <returns>True if should bother to proceed with copying.</returns>
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
                var segment = Segments[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);
                segment.CopyTo(segOffset, destination, destinationIndex, copyLength);
                count -= copyLength;
                destinationIndex += copyLength;
                segIndex++;
                segOffset = 0;
            }
        }

        internal static void AddSegments(ArrayBuilder<SourceText> segments, SourceText text)
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

        internal static SourceText ToSourceTextAndFree(ArrayBuilder<SourceText> segments, SourceText original, bool adjustSegments)
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

        // both of these numbers are currently arbitrary.
        internal const int TARGET_SEGMENT_COUNT_AFTER_REDUCTION = 32;
        internal const int MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION = 64;

        /// <summary>
        /// Reduces the number of segments toward the target number of segments,
        /// if the number of segments is deemed to be too large (beyond the maximum).
        /// </summary>
        /// <param name="segments"></param>
        private static void ReduceSegmentCountIfNecessary(ArrayBuilder<SourceText> segments)
        {
            if (segments.Count > MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION)
            {
                var segmentSize = GetMinimalSegmentSizeToUseForCombining(segments);
                CombineSegments(segments, segmentSize);
            }
        }

        // Allow combining segments if each has a size less than or equal to this amount.
        // This is some arbitrary number deemed to be small
        private const int INITIAL_SEGMENT_SIZE_FOR_COMBINING = 32;

        // Segments must be less than (or equal) to this size to be combined with other segments.
        // This is some arbitrary number that is a fraction of max int.
        private const int MAXIMUM_SEGMENT_SIZE_FOR_COMBINING = int.MaxValue / 16;

        /// <summary>
        /// Determines the segment size to use for call to CombineSegments, that will result in the segment count
        /// being reduced to less than or equal to the target segment count.
        /// </summary>
        /// <param name="segments"></param>
        private static int GetMinimalSegmentSizeToUseForCombining(ArrayBuilder<SourceText> segments)
        {
            // find the minimal segment size that reduces enough segments to less that or equal to the ideal segment count
            for (var segmentSize = INITIAL_SEGMENT_SIZE_FOR_COMBINING;
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

        /// <summary>
        /// Determines the segment count that would result if the segments of size less than or equal to 
        /// the specified segment size were to be combined.
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="segmentSize"></param>
        private static int GetSegmentCountIfCombined(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            int numberOfSegmentsReduced = 0;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    // count how many contiguous segments can be combined
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
                        var removed = count - 1;
                        numberOfSegmentsReduced += removed;
                        i += removed;
                    }
                }
            }

            return segments.Count - numberOfSegmentsReduced;
        }

        /// <summary>
        /// Combines contiguous segments with lengths that are each less than or equal to the specified segment size.
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="segmentSize"></param>
        private static void CombineSegments(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    int combinedLength = segments[i].Length;

                    // count how many contiguous segments are reducible
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length <= segmentSize)
                        {
                            count++;
                            combinedLength += segments[j].Length;
                        }
                    }

                    // if we've got at least two, then combine them into a single text
                    if (count > 1)
                    {
                        var encoding = segments[i].Encoding;
                        var algorithm = segments[i].ChecksumAlgorithm;
                        var writer = SourceTextWriter.Create(encoding, algorithm, combinedLength);
                        while (count > 0)
                        {
                            segments[i].Write(writer);
                            segments.RemoveAt(i);
                            count--;
                        }

                        var newText = writer.ToSourceText();
                        segments.Insert(i, newText);
                    }
                }
            }
        }

        private static readonly ObjectPool<HashSet<SourceText>> s_uniqueSourcesPool
            = new ObjectPool<HashSet<SourceText>>(() => new HashSet<SourceText>(), 5);

        /// <summary>
        /// Compute total text length and total size of storage buffers held
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="length"></param>
        /// <param name="size"></param>
        private static void ComputeLengthAndStorageSize(IReadOnlyList<SourceText> segments, out int length, out int size)
        {
            var uniqueSources = s_uniqueSourcesPool.Allocate();
            length = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                length += segment.Length;
                uniqueSources.Add(segment.StorageKey);
            }

            size = 0;
            foreach (var segment in uniqueSources)
            {
                size += segment.StorageSize;
            }

            uniqueSources.Clear();
            s_uniqueSourcesPool.Free(uniqueSources);
        }

        /// <summary>
        /// Trim excessive inaccessible text.
        /// </summary>
        /// <param name="segments"></param>
        private static void TrimInaccessibleText(ArrayBuilder<SourceText> segments)
        {
            int length, size;
            ComputeLengthAndStorageSize(segments, out length, out size);

            // if more than half of the storage is unused, compress into a single new segment
            if (length < size / 2)
            {
                var encoding = segments[0].Encoding;
                var algorithm = segments[0].ChecksumAlgorithm;
                var writer = SourceTextWriter.Create(encoding, algorithm, length);
                foreach (var segment in segments)
                {
                    segment.Write(writer);
                }

                segments.Clear();
                segments.Add(writer.ToSourceText());
            }
        }
    }
}
