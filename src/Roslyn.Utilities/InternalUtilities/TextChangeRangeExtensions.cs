using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    public static class TextChangeRangeExtensions
    {
        public static TextChangeRange? Accumulate(this TextChangeRange? accumulatedTextChangeSoFar,
            IEnumerable<TextChangeRange> changesInNextVersion)
        {
            if (!changesInNextVersion.Any())
            {
                return accumulatedTextChangeSoFar;
            }

            TextChangeRange newChange = TextChangeRange.Collapse(changesInNextVersion);
            if (accumulatedTextChangeSoFar == null)
            {
                return newChange;
            }

            int currentStart = accumulatedTextChangeSoFar.Value.Span.Start;
            int currentOldEnd = accumulatedTextChangeSoFar.Value.Span.End;
            int currentNewEnd = accumulatedTextChangeSoFar.Value.Span.Start + accumulatedTextChangeSoFar.Value.NewLength;
            if (newChange.Span.Start < currentStart)
            {
                currentStart = newChange.Span.Start;
            }

            if (currentNewEnd > newChange.Span.End)
            {
                currentNewEnd = currentNewEnd + newChange.NewLength - newChange.Span.Length;
            }
            else
            {
                currentOldEnd = currentOldEnd + newChange.Span.End - currentNewEnd;
                currentNewEnd = newChange.Span.Start + newChange.NewLength;
            }

            return new TextChangeRange(TextSpan.FromBounds(currentStart, currentOldEnd), currentNewEnd - currentStart);
        }
    }
}
