using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public struct TextLine : IEquatable<TextLine>
    {
        private readonly SourceText _text;
        private readonly int _start;
        private readonly int _endIncludingBreaks;

        private TextLine(SourceText text, int start, int endIncludingBreaks)
        {
            _text = text;
            _start = start;
            _endIncludingBreaks = endIncludingBreaks;
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

        public SourceText Text
        {
            get
            {
                return _text;
            }
        }

        public int LineNumber
        {
            get
            {
                return _text?.Lines.IndexOf(_start) ?? 0;
            }
        }

        public int Start
        {
            get
            {
                return _start;
            }
        }

        public int End
        {
            get
            {
                return _endIncludingBreaks - LineBreakLength;
            }
        }

        private int LineBreakLength
        {
            get
            {
                if (_text == null || _text.Length == 0 || _endIncludingBreaks == _start)
                {
                    return 0;
                }

                int startLineBreak;
                int lineBreakLength;
                TextUtilities.GetStartAndLengthOfLineBreakEndingAt(_text, _endIncludingBreaks - 1, out startLineBreak, out lineBreakLength);
                return lineBreakLength;
            }
        }

        public int EndIncludingLineBreak
        {
            get
            {
                return _endIncludingBreaks;
            }
        }

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
            if (_text == null || _text.Length == 0)
            {
                return string.Empty;
            }

            return _text.ToString(Span);
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
            return other._text == _text && other._start == _start && other._endIncludingBreaks == _endIncludingBreaks;
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
            return Hash.Combine(_text, Hash.Combine(_start, _endIncludingBreaks));
        }
    }
}
