using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Text
{
    public sealed class ChangedText : SourceText
    {
        private readonly SourceText _newText;
        private readonly ChangeInfo _info;

        public ChangedText(SourceText oldText, SourceText newText, ImmutableArray<TextChangeRange> changeRanges)
            : base(checksumAlgorithm: oldText.ChecksumAlgorithm)
        {
            Debug.Assert(newText != null);
            Debug.Assert(newText is CompositeText || newText is SubText || newText is StringText || newText is LargeText);
            Debug.Assert(oldText != null);
            Debug.Assert(oldText != newText);
            Debug.Assert(!changeRanges.IsDefault);
            _newText = newText;
            _info = new ChangeInfo(changeRanges, new WeakReference<SourceText>(oldText), (oldText as ChangedText)?._info);
        }

        private class ChangeInfo
        {
            public ImmutableArray<TextChangeRange> ChangeRanges { get; }

            public WeakReference<SourceText> WeakOldText { get; }

            public ChangeInfo Previous { get; private set; }

            public ChangeInfo(ImmutableArray<TextChangeRange> changeRanges, WeakReference<SourceText> weakOldText, ChangeInfo previous)
            {
                ChangeRanges = changeRanges;
                WeakOldText = weakOldText;
                Previous = previous;
                Clean();
            }

            private void Clean()
            {
                ChangeInfo lastInfo = this;
                for (ChangeInfo info = this; info != null; info = info.Previous)
                {
                    SourceText tmp;
                    if (info.WeakOldText.TryGetTarget(out tmp))
                    {
                        lastInfo = info;
                    }
                }

                ChangeInfo prev;
                while (lastInfo != null)
                {
                    prev = lastInfo.Previous;
                    lastInfo.Previous = null;
                    lastInfo = prev;
                }
            }
        }

        public override Encoding Encoding
        {
            get
            {
                return _newText.Encoding;
            }
        }

        public IEnumerable<TextChangeRange> Changes
        {
            get
            {
                return _info.ChangeRanges;
            }
        }

        public override int Length
        {
            get
            {
                return _newText.Length;
            }
        }

        internal override int StorageSize
        {
            get
            {
                return _newText.StorageSize;
            }
        }

        internal override ImmutableArray<SourceText> Segments
        {
            get
            {
                return _newText.Segments;
            }
        }

        internal override SourceText StorageKey
        {
            get
            {
                return _newText.StorageKey;
            }
        }

        public override char this[int position]
        {
            get
            {
                return _newText[position];
            }
        }

        public override string ToString(TextSpan span)
        {
            return _newText.ToString(span);
        }

        public override SourceText GetSubText(TextSpan span)
        {
            return _newText.GetSubText(span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _newText.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override SourceText WithChanges(IEnumerable<TextChange> changes)
        {
            ChangedText changed = _newText.WithChanges(changes) as ChangedText;
            if (changed != null)
            {
                return new ChangedText(this, changed._newText, changed._info.ChangeRanges);
            }

            return this;
        }

        public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (this == oldText)
            {
                return TextChangeRange.NoChanges;
            }

            SourceText actualOldText;
            if (_info.WeakOldText.TryGetTarget(out actualOldText) && actualOldText == oldText)
            {
                return _info.ChangeRanges;
            }

            if (IsChangedFrom(oldText))
            {
                IReadOnlyList<ImmutableArray<TextChangeRange>> changes = GetChangesBetween(oldText, this);
                if (changes.Count > 1)
                {
                    return Merge(changes);
                }
            }

            if (actualOldText?.GetChangeRanges(oldText).Count == 0)
            {
                return _info.ChangeRanges;
            }

            return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), _newText.Length));
        }

        private bool IsChangedFrom(SourceText oldText)
        {
            for (ChangeInfo info = _info; info != null; info = info.Previous)
            {
                SourceText text;
                if (info.WeakOldText.TryGetTarget(out text) && text == oldText)
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<ImmutableArray<TextChangeRange>> GetChangesBetween(SourceText oldText, ChangedText newText)
        {
            List<ImmutableArray<TextChangeRange>> list = new List<ImmutableArray<TextChangeRange>>();
            ChangeInfo change = newText._info;
            list.Add(change.ChangeRanges);
            while (change != null)
            {
                SourceText actualOldText;
                change.WeakOldText.TryGetTarget(out actualOldText);
                if (actualOldText == oldText)
                {
                    return list;
                }

                change = change.Previous;
                if (change != null)
                {
                    list.Insert(0, change.ChangeRanges);
                }
            }

            list.Clear();
            return list;
        }

        private static ImmutableArray<TextChangeRange> Merge(IReadOnlyList<ImmutableArray<TextChangeRange>> changeSets)
        {
            Debug.Assert(changeSets.Count > 1);
            ImmutableArray<TextChangeRange> merged = changeSets[0];
            for (int i = 1; i < changeSets.Count; i++)
            {
                merged = Merge(merged, changeSets[i]);
            }

            return merged;
        }

        private static ImmutableArray<TextChangeRange> Merge(ImmutableArray<TextChangeRange> oldChanges,
            ImmutableArray<TextChangeRange> newChanges)
        {
            List<TextChangeRange> list = new List<TextChangeRange>(oldChanges.Length + newChanges.Length);
            int oldIndex = 0;
            int newIndex = 0;
            int oldDelta = 0;
            nextNewChange:
            if (newIndex < newChanges.Length)
            {
                TextChangeRange newChange = newChanges[newIndex];
                nextOldChange:
                if (oldIndex < oldChanges.Length)
                {
                    TextChangeRange oldChange = oldChanges[oldIndex];
                    tryAgain:
                    if (oldChange.Span.Length == 0 && oldChange.NewLength == 0)
                    {
                        oldIndex++;
                        goto nextOldChange;
                    }

                    if (newChange.Span.Length == 0 && newChange.NewLength == 0)
                    {
                        newIndex++;
                        goto nextNewChange;
                    }

                    if (newChange.Span.End < oldChange.Span.Start + oldDelta)
                    {
                        TextChangeRange adjustedNewChange =
                            new TextChangeRange(new TextSpan(newChange.Span.Start - oldDelta, newChange.Span.Length), newChange.NewLength);
                        AddRange(list, adjustedNewChange);
                        newIndex++;
                        goto nextNewChange;
                    }

                    if (newChange.Span.Start > oldChange.Span.Start + oldDelta + oldChange.NewLength)
                    {
                        AddRange(list, oldChange);
                        oldDelta = oldDelta - oldChange.Span.Length + oldChange.NewLength;
                        oldIndex++;
                        goto nextOldChange;
                    }

                    if (newChange.Span.Start < oldChange.Span.Start + oldDelta)
                    {
                        int newChangeLeadingDeletion = oldChange.Span.Start + oldDelta - newChange.Span.Start;
                        AddRange(list, new TextChangeRange(new TextSpan(newChange.Span.Start - oldDelta, newChangeLeadingDeletion), 0));
                        newChange = new TextChangeRange(
                            new TextSpan(oldChange.Span.Start + oldDelta, newChange.Span.Length - newChangeLeadingDeletion),
                            newChange.NewLength);
                        goto tryAgain;
                    }

                    if (newChange.Span.Start > oldChange.Span.Start + oldDelta)
                    {
                        int oldChangeLeadingInsertion = newChange.Span.Start - (oldChange.Span.Start + oldDelta);
                        AddRange(list, new TextChangeRange(oldChange.Span, oldChangeLeadingInsertion));
                        oldDelta = oldDelta - oldChange.Span.Length + oldChangeLeadingInsertion;
                        oldChange = new TextChangeRange(new TextSpan(oldChange.Span.Start, 0),
                            oldChange.NewLength - oldChangeLeadingInsertion);
                        newChange = new TextChangeRange(new TextSpan(oldChange.Span.Start + oldDelta, newChange.Span.Length),
                            newChange.NewLength);
                        goto tryAgain;
                    }

                    if (newChange.Span.Start == oldChange.Span.Start + oldDelta)
                    {
                        if (oldChange.NewLength == 0)
                        {
                            AddRange(list, oldChange);
                            oldDelta = oldDelta - oldChange.Span.Length + oldChange.NewLength;
                            oldIndex++;
                            goto nextOldChange;
                        }

                        if (newChange.Span.Length == 0)
                        {
                            AddRange(list, new TextChangeRange(oldChange.Span, oldChange.NewLength + newChange.NewLength));
                            oldDelta = oldDelta - oldChange.Span.Length + oldChange.NewLength;
                            oldIndex++;
                            newIndex++;
                            goto nextNewChange;
                        }

                        int oldChangeReduction = Math.Min(oldChange.NewLength, newChange.Span.Length);
                        AddRange(list, new TextChangeRange(oldChange.Span, oldChange.NewLength - oldChangeReduction));
                        oldDelta = oldDelta - oldChange.Span.Length + (oldChange.NewLength - oldChangeReduction);
                        oldIndex++;
                        newChange = new TextChangeRange(
                            new TextSpan(oldChange.Span.Start + oldDelta, newChange.Span.Length - oldChangeReduction),
                            newChange.NewLength);
                        goto nextOldChange;
                    }
                }
                else
                {
                    TextChangeRange adjustedNewChange =
                        new TextChangeRange(new TextSpan(newChange.Span.Start - oldDelta, newChange.Span.Length), newChange.NewLength);
                    AddRange(list, adjustedNewChange);
                    newIndex++;
                    goto nextNewChange;
                }
            }
            else
            {
                while (oldIndex < oldChanges.Length)
                {
                    AddRange(list, oldChanges[oldIndex]);
                    oldIndex++;
                }
            }

            return list.ToImmutableArray();
        }

        private static void AddRange(List<TextChangeRange> list, TextChangeRange range)
        {
            if (list.Count > 0)
            {
                TextChangeRange last = list[list.Count - 1];
                if (last.Span.End == range.Span.Start)
                {
                    list[list.Count - 1] = new TextChangeRange(new TextSpan(last.Span.Start, last.Span.Length + range.Span.Length),
                        last.NewLength + range.NewLength);
                    return;
                }

                Debug.Assert(range.Span.Start > last.Span.End);
            }

            list.Add(range);
        }

        protected override TextLineCollection GetLinesCore()
        {
            SourceText oldText;
            TextLineCollection oldLineInfo;
            if (!_info.WeakOldText.TryGetTarget(out oldText) || !oldText.TryGetLines(out oldLineInfo))
            {
                return base.GetLinesCore();
            }

            ArrayBuilder<int> lineStarts = ArrayBuilder<int>.GetInstance();
            lineStarts.Add(0);
            int position = 0;
            int delta = 0;
            bool endsWithCR = false;
            foreach (TextChangeRange change in _info.ChangeRanges)
            {
                if (change.Span.Start > position)
                {
                    if (endsWithCR && _newText[position + delta] == '\n')
                    {
                        lineStarts.RemoveLast();
                    }

                    LinePositionSpan lps = oldLineInfo.GetLinePositionSpan(TextSpan.FromBounds(position, change.Span.Start));
                    for (int i = lps.Start.Line + 1; i <= lps.End.Line; i++)
                    {
                        lineStarts.Add(oldLineInfo[i].Start + delta);
                    }

                    endsWithCR = oldText[change.Span.Start - 1] == '\r';
                    if (endsWithCR && change.Span.Start < oldText.Length && oldText[change.Span.Start] == '\n')
                    {
                        lineStarts.Add(change.Span.Start + delta);
                    }
                }

                if (change.NewLength > 0)
                {
                    int changeStart = change.Span.Start + delta;
                    SourceText text = GetSubText(new TextSpan(changeStart, change.NewLength));
                    if (endsWithCR && text[0] == '\n')
                    {
                        lineStarts.RemoveLast();
                    }

                    for (int i = 1; i < text.Lines.Count; i++)
                    {
                        lineStarts.Add(changeStart + text.Lines[i].Start);
                    }

                    endsWithCR = text[change.NewLength - 1] == '\r';
                }

                position = change.Span.End;
                delta += change.NewLength - change.Span.Length;
            }

            if (position < oldText.Length)
            {
                if (endsWithCR && _newText[position + delta] == '\n')
                {
                    lineStarts.RemoveLast();
                }

                LinePositionSpan lps = oldLineInfo.GetLinePositionSpan(TextSpan.FromBounds(position, oldText.Length));
                for (int i = lps.Start.Line + 1; i <= lps.End.Line; i++)
                {
                    lineStarts.Add(oldLineInfo[i].Start + delta);
                }
            }

            return new LineInfo(this, lineStarts.ToArrayAndFree());
        }
    }
}
