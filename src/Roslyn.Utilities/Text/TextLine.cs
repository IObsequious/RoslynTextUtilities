using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct TextLine : IEquatable<TextLine>
    {
        private TextLine(SourceText text, int start, int endIncludingBreaks)
        {
            Text = text;
            Start = start;
            EndIncludingLineBreak = endIncludingBreaks;
        }

        public static TextLine FromSpan(SourceText text, TextSpan span)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (span.Start > text.Length || span.Start < 0 || span.End > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (text.Length > 0)
            {
                if (span.Start > 0 && !TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]))
                {
                    throw new ArgumentOutOfRangeException(nameof(span), CodeAnalysisResources.SpanDoesNotIncludeStartOfLine);
                }

                bool endIncludesLineBreak = false;
                if (span.End > span.Start)
                {
                    endIncludesLineBreak = TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]);
                }

                if (!endIncludesLineBreak && span.End < text.Length)
                {
                    int lineBreakLen = TextUtilities.GetLengthOfLineBreak(text, span.End);
                    if (lineBreakLen > 0)
                    {
                        endIncludesLineBreak = true;
                        span = new TextSpan(span.Start, span.Length + lineBreakLen);
                    }
                }

                if (span.End < text.Length && !endIncludesLineBreak)
                {
                    throw new ArgumentOutOfRangeException(nameof(span), CodeAnalysisResources.SpanDoesNotIncludeEndOfLine);
                }

                return new TextLine(text, span.Start, span.End);
            }

            return new TextLine(text, 0, 0);
        }

        public SourceText Text { get; }

        public int LineNumber
        {
            get
            {
                return Text?.Lines.IndexOf(Start) ?? 0;
            }
        }

        public int Start { get; }

        public int End
        {
            get
            {
                return EndIncludingLineBreak - LineBreakLength;
            }
        }

        private int LineBreakLength
        {
            get
            {
                if (Text == null || Text.Length == 0 || EndIncludingLineBreak == Start)
                {
                    return 0;
                }

                int startLineBreak;
                int lineBreakLength;
                TextUtilities.GetStartAndLengthOfLineBreakEndingAt(Text, EndIncludingLineBreak - 1, out startLineBreak, out lineBreakLength);
                return lineBreakLength;
            }
        }

        public int EndIncludingLineBreak { get; }

        public TextSpan Span
        {
            get
            {
                return TextSpan.FromBounds(Start, End);
            }
        }

        public TextSpan SpanIncludingLineBreak
        {
            get
            {
                return TextSpan.FromBounds(Start, EndIncludingLineBreak);
            }
        }

        public override string ToString()
        {
            if (Text == null || Text.Length == 0)
            {
                return string.Empty;
            }

            return Text.ToString(Span);
        }

        public static bool operator ==(TextLine left, TextLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextLine left, TextLine right)
        {
            return !left.Equals(right);
        }

        public bool Equals(TextLine other)
        {
            return other.Text == Text && other.Start == Start && other.EndIncludingLineBreak == EndIncludingLineBreak;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextLine)
            {
                return Equals((TextLine) obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Text, Hash.Combine(Start, EndIncludingLineBreak));
        }
    }
}
